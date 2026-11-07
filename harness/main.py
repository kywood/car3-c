
import subprocess
import time
from pathlib import Path

from core.todo_manager import TodoManager
from core.coding_agent import CodingAgent
from core.validator import UnityValidator
from core.review_manager import ReviewManager

from slack_reporter import (
    send_message,
    send_screenshot,
    send_review_request,
)
from reporters.screenshot import capture_screen


BASE_DIR = Path(__file__).resolve().parent
PROJECT_DIR = BASE_DIR.parent

REPORT_INTERVAL = 300
CODEX_TIMEOUT = 900
BUILD_TIMEOUT = 1200
MAX_RETRIES = 3
REVIEW_POLL_INTERVAL = 5

EXPECTED_BRANCH = "fix/near-miss-detection"


def current_branch() -> str:
    result = subprocess.run(
        ["git", "branch", "--show-current"],
        cwd=PROJECT_DIR,
        capture_output=True,
        text=True,
        check=True,
    )
    return result.stdout.strip()


def report_screenshot(message: str):
    try:
        image_path = capture_screen()
        send_screenshot(image_path, message)
    except Exception as e:
        print(f"스크린샷 실패: {e}", flush=True)


def report_progress(task: str, elapsed: int):
    send_message(
        f"🔄 Claude Code 작업 진행 중\n"
        f"TODO: {task[:500]}\n"
        f"경과 시간: {elapsed // 60}분"
    )

    report_screenshot(
        f"📸 Unity Harness 진행 화면\n"
        f"TODO: {task[:500]}"
    )


def wait_for_review(review: ReviewManager, task: str) -> str:
    print("Slack 사용자 승인 대기...", flush=True)

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
        f"원래 TODO:\n{task}\n\n"
        "Unity Android 빌드가 실패했다.\n"
        "기존 변경 사항을 유지하면서 빌드 오류만 수정하라.\n"
        "CLAUDE.md 지침을 준수하라.\n"
        "Git 브랜치 변경, Commit, Push는 하지 마라.\n\n"
        f"빌드 오류:\n{error_log[-6000:]}"
    )


def execute_task(task: str, review: ReviewManager) -> bool:
    status = review.get_status(task)

    if status == "approved":
        print("기존 승인 결과 확인", flush=True)
        return True

    if status == "pending":
        print("기존 Slack 승인 요청 대기", flush=True)

    elif status == "rejected":
        send_message(
            f"⛔ 이전에 거절된 TODO\n"
            f"작업: {task}\n"
            "재작업 지시가 필요합니다."
        )
        return False

    else:
        agent = CodingAgent(PROJECT_DIR)
        validator = UnityValidator(PROJECT_DIR)

        error_log = None

        for attempt in range(1, MAX_RETRIES + 1):
            send_message(
                f"🤖 Claude Code 개발 시작\n"
                f"TODO: {task}\n"
                f"시도: {attempt}/{MAX_RETRIES}"
            )

            try:
                result = agent.execute(
                    make_prompt(task, error_log),
                    timeout=CODEX_TIMEOUT,
                    on_progress=report_progress,
                    report_interval=REPORT_INTERVAL,
                )
            except Exception as e:
                print(f"Claude 실행 오류: {e}", flush=True)
                send_message(
                    f"❌ Claude Code 실행 오류\n"
                    f"{str(e)[:500]}"
                )
                return False

            print(f"Claude Code 상태: {result.status}")
            print(f"로그: {result.log_path}")

            if result.status == "usage_limit":
                send_message(
                    f"⏸️ Claude Code 사용량 제한\n"
                    f"TODO: {task}\n"
                    "작업을 중단합니다."
                )
                return False

            if result.status != "success":
                send_message(
                    f"❌ Claude Code 작업 실패\n"
                    f"TODO: {task}\n"
                    f"상태: {result.status}"
                )
                return False

            send_message(
                f"🔨 Unity Android 빌드 시작\n"
                f"TODO: {task}"
            )

            try:
                validation = validator.validate(
                    timeout=BUILD_TIMEOUT
                )
            except Exception as e:
                send_message(
                    f"❌ Unity 빌드 실행 오류\n"
                    f"{str(e)[:500]}"
                )
                return False

            if validation.success:
                print("Unity 빌드 성공", flush=True)

                review.request(task)

                if not send_review_request(task):
                    print("Slack 승인 요청 전송 실패")
                    return False

                report_screenshot(
                    f"📸 Unity Android 빌드 성공\n"
                    f"TODO: {task}"
                )
                break

            error_log = validation.output[-6000:]

            send_message(
                f"⚠️ Unity 빌드 실패\n"
                f"시도: {attempt}/{MAX_RETRIES}\n"
                f"오류: {error_log[-1000:]}"
            )

        else:
            send_message(
                f"🚨 Unity 빌드 {MAX_RETRIES}회 실패\n"
                f"TODO: {task}\n"
                "사용자 확인이 필요합니다."
            )
            return False

    decision = wait_for_review(review, task)

    if decision == "approved":
        send_message(
            f"✅ 사용자 승인 완료\n"
            f"TODO: {task}"
        )
        return True

    send_message(
        f"⛔ 작업 승인되지 않음\n"
        f"TODO: {task}\n"
        f"상태: {decision}"
    )
    return False


def main():
    print("=== Unity Claude Harness 시작 ===", flush=True)

    branch = current_branch()

    if branch != EXPECTED_BRANCH:
        print(
            f"잘못된 Git 브랜치: {branch}\n"
            f"필요한 브랜치: {EXPECTED_BRANCH}",
            flush=True,
        )
        return

    manager = TodoManager(BASE_DIR / "TODO.md")
    review = ReviewManager(BASE_DIR)

    todo = manager.get_next_todo()

    if todo is None:
        print("모든 TODO 완료", flush=True)
        send_message("✅ 모든 TODO 완료")
        return

    task = todo.description

    print(f"현재 브랜치: {branch}", flush=True)
    print(f"현재 TODO: {task}", flush=True)

    send_message(
        f"🚀 Unity Claude Harness 시작\n"
        f"브랜치: {branch}\n"
        f"TODO: {task}"
    )

    try:
        success = execute_task(task, review)

        if not success:
            print("작업 중단 — TODO 미완료 유지")
            return

        # 승인된 TODO만 완료 처리
        manager.complete(todo)

        send_message(
            f"✅ TODO 완료\n"
            f"브랜치: {branch}\n"
            f"작업: {task}\n"
            "다음 TODO는 자동 실행하지 않습니다."
        )

        print("현재 TODO 완료. Harness 종료.")

    except KeyboardInterrupt:
        print("Harness 수동 중단", flush=True)
        send_message("⏹️ Unity Harness 수동 중단")

    except Exception as e:
        print(f"Harness 오류: {e}", flush=True)
        send_message(
            f"🚨 Harness 오류\n{str(e)[:500]}"
        )
        raise

    finally:
        print("=== Unity Claude Harness 종료 ===", flush=True)


if __name__ == "__main__":
    main()
