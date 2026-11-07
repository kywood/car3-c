
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


def report_screenshot(message: str):
    try:
        image_path = capture_screen()
        send_screenshot(image_path, message)
    except Exception as e:
        print(f"스크린샷 실패: {e}", flush=True)


def report_progress(task: str, elapsed: int):
    send_message(
        f"🔄 Harness 작업 중\n"
        f"TODO: {task[:500]}\n"
        f"경과 시간: {elapsed // 60}분"
    )

    report_screenshot(
        f"📸 진행 화면: {task[:500]}"
    )


def wait_for_review(review: ReviewManager, task: str) -> str:
    """Slack 사용자 승인 또는 거절 대기."""
    print("Slack 승인 대기 중...", flush=True)

    while True:
        status = review.get_status(task)

        if status == "approved":
            return "approved"

        if status == "rejected":
            return "rejected"

        if status != "pending":
            return "invalid"

        time.sleep(REVIEW_POLL_INTERVAL)


def make_prompt(task: str, error_log: str | None) -> str:
    if not error_log:
        return task

    return (
        f"원래 TODO:\n{task}\n\n"
        "이전 Unity Android 빌드가 실패했다.\n"
        "기능 요구사항을 유지하면서 빌드 오류를 수정하라.\n"
        "불필요한 변경과 Git 커밋은 하지 마라.\n\n"
        f"빌드 오류:\n{error_log[-6000:]}"
    )


def execute_task(task: str, review: ReviewManager) -> bool:
    """TODO 하나를 개발하고 승인까지 처리."""

    # 기존 대기 상태가 있으면 재개
    status = review.get_status(task)

    if status == "pending":
        print("기존 승인 대기 작업 재개", flush=True)

    elif status == "approved":
        print("기존 승인 결과 확인", flush=True)

    elif status == "rejected":
        send_message(
            f"❌ 사용자 거절로 작업 중단\n"
            f"TODO: {task}"
        )
        return False

    else:
        agent = CodingAgent(PROJECT_DIR)
        validator = UnityValidator(PROJECT_DIR)

        error_log = None

        for attempt in range(1, MAX_RETRIES + 1):
            send_message(
                f"🤖 Codex 개발 시작\n"
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
                send_message(f"❌ Codex 실행 오류: {str(e)[:500]}")
                return False

            if result.status == "usage_limit":
                send_message(
                    f"⏸️ Codex 사용량 제한\n"
                    f"TODO: {task}\n"
                    "Harness 실행을 중단합니다."
                )
                return False

            if result.status != "success":
                send_message(
                    f"❌ Codex 실행 실패\n"
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
                    f"❌ Validator 오류: {str(e)[:500]}"
                )
                return False

            if validation.success:
                print("Unity Android 빌드 성공!", flush=True)

                # 승인 요청 상태 먼저 저장
                review.request(task)

                # 실제 Slack 승인 버튼 전송
                sent = send_review_request(task)

                if not sent:
                    print("승인 버튼 전송 실패", flush=True)
                    return False

                report_screenshot(
                    f"📸 Unity 빌드 완료\nTODO: {task}"
                )
                break

            error_log = validation.output[-6000:]

            send_message(
                f"⚠️ 빌드 실패 — 자동 수정 대상\n"
                f"시도: {attempt}/{MAX_RETRIES}\n"
                f"오류: {error_log[-1000:]}"
            )

        else:
            send_message(
                f"🚨 최대 {MAX_RETRIES}회 빌드 실패\n"
                f"TODO: {task}\n"
                "사용자 확인 필요"
            )
            return False

    # Slack에서 승인/거절 클릭을 기다림
    decision = wait_for_review(review, task)

    if decision == "approved":
        send_message(
            f"✅ 사용자 승인 확인\n"
            f"TODO: {task}"
        )
        return True

    send_message(
        f"⛔ 작업 중단\n"
        f"TODO: {task}\n"
        f"검증 결과: {decision}"
    )
    return False


def main():
    print("=== Unity AI Harness 자동 개발 시작 ===", flush=True)

    manager = TodoManager(BASE_DIR / "TODO.md")
    review = ReviewManager(BASE_DIR)

    send_message("🚀 Unity AI Harness 자동 개발 시작")

    try:
        while True:
            todos = manager.get_todos()
            completed = sum(todo.completed for todo in todos)

            todo = manager.get_next_todo()

            if todo is None:
                send_message(
                    f"🎉 모든 TODO 완료!\n"
                    f"완료: {completed}/{len(todos)}"
                )
                print("모든 작업 완료!")
                break

            task = todo.description

            print(
                f"\n[{completed}/{len(todos)}] {task}",
                flush=True
            )

            send_message(
                f"📋 다음 작업\n"
                f"진행률: {completed}/{len(todos)}\n"
                f"TODO: {task}"
            )

            # 개발 → 빌드 → Slack 승인
            success = execute_task(task, review)

            if not success:
                print("작업 중단. TODO 미완료 유지.")
                break

            # 승인된 작업만 완료 처리
            manager.complete(todo)

            send_message(
                f"✅ TODO 완료 처리\n"
                f"TODO: {task}\n"
                "다음 작업으로 이동합니다."
            )

            # 다음 TODO로 자동 반복
            time.sleep(2)

    except KeyboardInterrupt:
        print("\n사용자가 Harness를 중단했습니다.")
        send_message("⏹️ Unity Harness 수동 중단")

    except Exception as e:
        print(f"Harness 오류: {e}", flush=True)
        send_message(
            f"🚨 Harness 오류로 중단\n"
            f"{str(e)[:500]}"
        )
        raise

    finally:
        print("=== Unity AI Harness 종료 ===", flush=True)


if __name__ == "__main__":
    main()
