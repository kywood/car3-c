
import subprocess
import time
from pathlib import Path

from core.todo_manager import TodoManager
from core.coding_agent import CodingAgent
from core.validator import UnityValidator
from core.review_manager import ReviewManager
from core.git_manager import GitManager
from core.github_issue_manager import GitHubIssueManager, Issue

from slack_reporter import (
    send_message,
    send_screenshot,
    send_review_request,
)
from reporters.screenshot import capture_screen


BASE_DIR = Path(__file__).resolve().parent
PROJECT_DIR = BASE_DIR.parent

REPORT_INTERVAL = 300
CLAUDE_TIMEOUT = 900
BUILD_TIMEOUT = 1200
MAX_RETRIES = 3
REVIEW_POLL_INTERVAL = 5


def notify(message: str):
    print(message, flush=True)
    try:
        send_message(message)
    except Exception as e:
        print(f"Slack 보고 오류: {e}", flush=True)


def report_screenshot(message: str):
    try:
        path = capture_screen()
        send_screenshot(path, message)
    except Exception as e:
        print(f"스크린샷 오류: {e}", flush=True)


def report_progress(task: str, elapsed: int):
    notify(
        f"🔄 Claude Code 작업 중\n"
        f"TODO: {task[:400]}\n"
        f"경과 시간: {elapsed // 60}분"
    )
    report_screenshot(f"📸 작업 진행: {task[:400]}")


def wait_for_review(review: ReviewManager, task: str) -> str:
    print("Slack 승인 대기...", flush=True)

    while True:
        status = review.get_status(task)

        if status in ("approved", "rejected"):
            return status

        if status != "pending":
            return "invalid"

        time.sleep(REVIEW_POLL_INTERVAL)


def make_prompt(task: str, error_log: str | None) -> str:
    if not error_log:
        return task

    return (
        f"원래 TODO: {task}\n\n"
        "이전 Unity 빌드가 실패했다.\n"
        "기능 요구사항을 유지하면서 오류를 수정하라.\n"
        "CLAUDE.md 지침을 준수하라.\n"
        "Git 브랜치 변경, 커밋, Push는 하지 마라.\n\n"
        f"Unity 빌드 오류:\n{error_log[-6000:]}"
    )


def run_development(task: str, review: ReviewManager) -> bool:
    """Claude 개발 → Unity 빌드 → Slack 승인."""

    status = review.get_status(task)

    # 재시작 시 이미 승인된 작업 복구
    if status == "approved":
        return True

    if status == "rejected":
        notify(
            f"⛔ 이전에 거절된 작업\n"
            f"TODO: {task}\n"
            "사용자 재작업 지시 필요"
        )
        return False

    if status != "pending":
        agent = CodingAgent(PROJECT_DIR)
        validator = UnityValidator(PROJECT_DIR)
        error_log = None

        for attempt in range(1, MAX_RETRIES + 1):
            notify(
                f"🤖 Claude Code 개발 시작\n"
                f"TODO: {task}\n"
                f"시도: {attempt}/{MAX_RETRIES}"
            )

            try:
                result = agent.execute(
                    make_prompt(task, error_log),
                    timeout=CLAUDE_TIMEOUT,
                    on_progress=report_progress,
                    report_interval=REPORT_INTERVAL,
                )
            except Exception as e:
                notify(f"❌ Claude 실행 오류: {str(e)[:500]}")
                return False

            if result.status == "usage_limit":
                notify(
                    f"⏸️ Claude 사용량 제한\n"
                    f"TODO: {task}\n"
                    "작업을 안전하게 중단합니다."
                )
                return False

            if result.status != "success":
                notify(
                    f"❌ Claude 실행 실패\n"
                    f"TODO: {task}\n"
                    f"상태: {result.status}"
                )
                return False

            notify(
                f"🔨 Unity Android 빌드 시작\n"
                f"TODO: {task}"
            )

            try:
                validation = validator.validate(
                    timeout=BUILD_TIMEOUT
                )
            except Exception as e:
                notify(
                    f"❌ Unity Validator 오류: {str(e)[:500]}"
                )
                return False

            if validation.success:
                review.request(task)

                if not send_review_request(task):
                    notify("❌ Slack 승인 버튼 전송 실패")
                    return False

                report_screenshot(
                    f"📸 Unity 빌드 후 화면\nTODO: {task}"
                )
                break

            error_log = validation.output[-6000:]

            notify(
                f"⚠️ Unity 빌드 실패\n"
                f"TODO: {task}\n"
                f"시도: {attempt}/{MAX_RETRIES}\n"
                f"오류:\n{error_log[-1000:]}"
            )

        else:
            notify(
                f"🚨 Unity 빌드 {MAX_RETRIES}회 실패\n"
                f"TODO: {task}"
            )
            return False

    decision = wait_for_review(review, task)

    if decision != "approved":
        notify(
            f"⛔ 작업 중단\n"
            f"TODO: {task}\n"
            f"승인 결과: {decision}"
        )
        return False

    return True


def prepare_git_branch(git: GitManager, task: str, issue: Issue) -> str:
    """이슈마다 고유한 작업 브랜치(feature/issue-<번호>-...)를 준비하고 원격에 올린다."""

    branch = git.branch_for_issue(issue.number, task)

    # 다른 작업의 변경 사항을 보호
    current = git.current_branch()

    if current != branch and git.has_changes():
        raise RuntimeError(
            "미커밋 변경 사항이 있습니다. "
            "브랜치 전환 전에 Git 상태를 정리하세요."
        )

    git.prepare_branch(branch)

    if git.current_branch() != branch:
        raise RuntimeError("Git 브랜치 전환 실패")

    # 작업 시작 시점에 브랜치를 GitHub에 올려 이슈와 연결한다.
    if not git.remote_branch_exists(branch):
        git.push()

    return branch


def save_to_git(
    git: GitManager,
    task: str,
    branch: str,
    issue: Issue,
    issues: GitHubIssueManager,
):
    """승인된 작업을 Commit + Push. 커밋에 이슈 번호를 남겨 main 머지 시 이슈가 닫히게 한다."""

    if git.current_branch() != branch:
        raise RuntimeError("작업 중 Git 브랜치가 변경됐습니다.")

    message = (
        f"feat: {issue.title} (#{issue.number})\n\n"
        f"Closes #{issue.number}"
    )

    committed = git.commit(message)

    # 변경 파일이 없더라도 브랜치 Push
    git.push()

    issues.comment(
        issue.number,
        f"✅ 작업 완료, 브랜치 `{branch}`에 Push했습니다. "
        f"(새 커밋: {'있음' if committed else '없음'})\n"
        "main 머지는 사용자 승인 후 진행합니다.",
    )

    notify(
        f"📦 GitHub Push 완료\n"
        f"이슈: #{issue.number} {issue.url}\n"
        f"브랜치: {branch}\n"
        f"TODO: {task}\n"
        f"새 커밋: {'있음' if committed else '없음'}"
    )


def main():
    print("=== Unity Claude AI Harness ===", flush=True)

    git = GitManager(PROJECT_DIR)
    issues = GitHubIssueManager(PROJECT_DIR)
    manager = TodoManager(BASE_DIR / "TODO.md")
    review = ReviewManager(BASE_DIR)

    # 시작 전 안전 검사
    if git.current_branch() in ("main", "master"):
        # 새로운 작업 브랜치를 만드는 것은 허용
        # 단, 기본 브랜치의 미커밋 수정은 금지
        if git.has_changes():
            raise RuntimeError(
                "main/master에 미커밋 변경 사항이 있습니다."
            )

    # 원격 저장소 확인
    remote = subprocess.run(
        ["git", "remote", "get-url", "origin"],
        cwd=PROJECT_DIR,
        capture_output=True,
        text=True,
    )

    if remote.returncode != 0:
        raise RuntimeError("Git origin 원격 저장소가 없습니다.")

    notify("🚀 Unity Claude AI Harness 자동 개발 시작")

    try:
        while True:
            todo = manager.get_next_todo()

            if todo is None:
                notify("🎉 모든 TODO 완료!")
                break

            task = todo.description

            # 1. 요청(TODO)마다 GitHub 이슈 + 작업 브랜치 준비
            issue = issues.ensure_issue(task, source="harness/TODO.md")
            branch = prepare_git_branch(git, task, issue)

            if issue.created:
                issues.comment(issue.number, f"🌿 작업 브랜치 `{branch}`를 만들어 Push했습니다.")

            notify(
                f"📋 새로운 TODO\n"
                f"이슈: #{issue.number} {issue.url}\n"
                f"브랜치: {branch}\n"
                f"작업: {task}"
            )

            # 2. Claude 개발 + Unity 빌드 + Slack 승인
            success = run_development(task, review)

            if not success:
                print("TODO 미완료. Harness 중단.")
                break

            # 3. 승인된 TODO 완료 처리
            manager.complete(todo)

            # 4. Commit + Push
            save_to_git(git, task, branch, issue, issues)

            # 5. 다음 TODO로 반복
            print("다음 TODO 진행...", flush=True)
            time.sleep(2)

    except KeyboardInterrupt:
        notify("⏹️ Unity Harness 수동 중단")

    except Exception as e:
        notify(f"🚨 Harness 오류\n{str(e)[:500]}")
        raise

    finally:
        print("=== Harness 종료 ===", flush=True)


if __name__ == "__main__":
    main()
