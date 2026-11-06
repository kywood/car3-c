
from pathlib import Path

from core.todo_manager import TodoManager
from core.coding_agent import CodingAgent
from core.validator import UnityValidator
from core.review_manager import ReviewManager

from slack_reporter import send_message, send_screenshot
from reporters.screenshot import capture_screen


BASE_DIR = Path(__file__).resolve().parent
PROJECT_DIR = BASE_DIR.parent

REPORT_INTERVAL = 300   # 5분마다 진행 보고
CODEX_TIMEOUT = 900     # Codex 15분
BUILD_TIMEOUT = 1200    # Unity 빌드 20분
MAX_RETRIES = 3         # 최대 3회 시도


def report_progress(task: str, elapsed: int):
    """Codex 작업 중간 보고."""
    send_message(
        f"🔄 Unity Harness 작업 진행 중\n"
        f"TODO: {task}\n"
        f"경과 시간: {elapsed // 60}분"
    )

    try:
        image_path = capture_screen()
        send_screenshot(
            image_path,
            f"📸 Unity 작업 화면\n"
            f"TODO: {task}\n"
            f"경과 시간: {elapsed // 60}분"
        )
    except Exception as e:
        print(f"스크린샷 전송 실패: {e}")


def report_screenshot(message: str):
    """Windows 화면 캡처 및 Slack 전송."""
    try:
        image_path = capture_screen()
        send_screenshot(image_path, message)
    except Exception as e:
        print(f"스크린샷 전송 실패: {e}")


def make_prompt(task: str, error_log: str | None) -> str:
    """최초 작업 또는 빌드 오류 수정 요청 생성."""
    if not error_log:
        return task

    return (
        f"원래 개발 TODO:\n{task}\n\n"
        "이전 작업 이후 Unity Android 빌드가 실패했다.\n"
        "기존 변경 사항과 오류 로그를 분석하고 수정하라.\n"
        "기능 요구사항을 유지하라.\n"
        "불필요한 리팩터링은 하지 마라.\n"
        "Git 커밋은 하지 마라.\n\n"
        f"Unity 빌드 오류 로그:\n{error_log[-6000:]}"
    )


def main():
    print("=== Unity Harness 시작 ===", flush=True)

    # 1. 관리자 초기화
    manager = TodoManager(BASE_DIR / "TODO.md")
    review = ReviewManager(BASE_DIR)

    todos = manager.get_todos()
    completed = sum(todo.completed for todo in todos)

    print(f"전체 TODO: {len(todos)}")
    print(f"완료 TODO: {completed}")

    # 2. 다음 미완료 TODO
    todo = manager.get_next_todo()

    if todo is None:
        print("모든 TODO 완료", flush=True)
        send_message("✅ Unity Harness: 모든 TODO 완료!")
        return

    task = todo.description
    print(f"현재 작업: {task}", flush=True)

    # 3. 기존 승인 상태 확인
    review_status = review.get_status(task)

    if review_status == "pending":
        print("사용자 승인 대기 중 — Codex 재실행하지 않음")
        send_message(
            f"⏳ Unity Harness 승인 대기\n"
            f"TODO: {task}\n"
            f"상태: 게임 동작 검증 대기\n"
            f"중복 실행을 방지했습니다."
        )
        return

    if review_status == "approved":
        print("사용자 승인 상태 확인")
        manager.complete(todo)

        send_message(
            f"✅ TODO 완료 처리\n"
            f"TODO: {task}"
        )
        return

    if review_status == "rejected":
        print("이전 검증이 거절된 작업입니다.")
        send_message(
            f"🔁 이전 검증 거절 — 수정 재시도\n"
            f"TODO: {task}"
        )

    # 4. 작업 시작 보고
    send_message(
        f"🚀 Unity Harness 작업 시작\n"
        f"프로젝트: car3-c\n"
        f"진행률: {completed}/{len(todos)}\n"
        f"TODO: {task}"
    )

    agent = CodingAgent(PROJECT_DIR)
    validator = UnityValidator(PROJECT_DIR)

    error_log = None

    # 5. Codex 수정 → Unity 빌드 루프
    for attempt in range(1, MAX_RETRIES + 1):
        print(
            f"\n=== 작업 시도 {attempt}/{MAX_RETRIES} ===",
            flush=True
        )

        send_message(
            f"🤖 Codex 작업 시작\n"
            f"TODO: {task}\n"
            f"시도: {attempt}/{MAX_RETRIES}"
        )

        prompt = make_prompt(task, error_log)

        # Codex 실행
        try:
            result = agent.execute(
                prompt,
                timeout=CODEX_TIMEOUT,
                on_progress=report_progress,
                report_interval=REPORT_INTERVAL,
            )
        except Exception as e:
            print(f"Codex 실행 오류: {e}")
            send_message(
                f"❌ Codex 실행 오류\n"
                f"TODO: {task}\n"
                f"오류: {str(e)[:500]}"
            )
            return

        print(f"Codex 상태: {result.status}")
        print(f"Codex 로그: {result.log_path}")

        # Codex 사용량 제한
        if result.status == "usage_limit":
            send_message(
                f"⏸️ Codex 사용량 제한\n"
                f"TODO: {task}\n"
                f"상태: 작업 중단 — TODO 미완료 유지"
            )
            return

        # Codex 시간 초과
        if result.status == "timeout":
            send_message(
                f"⏱️ Codex 실행 시간 초과\n"
                f"TODO: {task}"
            )
            return

        # 기타 실행 실패
        if result.status != "success":
            send_message(
                f"❌ Codex 작업 실패\n"
                f"TODO: {task}\n"
                f"종료 코드: {result.returncode}"
            )
            return

        # 6. Unity 빌드 검증
        send_message(
            f"🔨 Unity Android 빌드 시작\n"
            f"TODO: {task}\n"
            f"시도: {attempt}/{MAX_RETRIES}"
        )

        try:
            validation = validator.validate(
                timeout=BUILD_TIMEOUT
            )
        except Exception as e:
            print(f"Unity 검증 오류: {e}")
            send_message(
                f"❌ Unity 검증 오류\n"
                f"TODO: {task}\n"
                f"오류: {str(e)[:500]}"
            )
            return

        print(f"Unity 빌드 성공: {validation.success}")
        print(f"Unity 빌드 로그: {validation.log_path}")

        # 7. 빌드 성공 → 승인 요청
        if validation.success:
            print("Unity 빌드 검증 성공!", flush=True)

            review.request(task)

            send_message(
                f"✅ Unity Android 빌드 성공\n"
                f"📋 게임 동작 검증 요청\n"
                f"프로젝트: car3-c\n"
                f"TODO: {task}\n"
                f"시도: {attempt}/{MAX_RETRIES}\n"
                f"상태: 사용자 승인 대기\n\n"
                f"게임을 실행해서 기능이 정상인지 확인해 주세요."
            )

            report_screenshot(
                f"📸 Unity 빌드 후 Windows 화면\n"
                f"TODO: {task}"
            )

            print("사용자 승인 대기", flush=True)
            return

        # 8. 빌드 실패 → 오류를 Codex에 전달
        error_log = validation.output[-6000:]

        print("Unity 빌드 실패", flush=True)

        send_message(
            f"⚠️ Unity Android 빌드 실패\n"
            f"TODO: {task}\n"
            f"시도: {attempt}/{MAX_RETRIES}\n"
            f"종료 코드: {validation.returncode}\n"
            f"로그: {validation.log_path}\n"
            f"최근 오류:\n{error_log[-1200:]}"
        )

        if attempt < MAX_RETRIES:
            send_message(
                f"🔁 Codex 자동 수정 재시도\n"
                f"TODO: {task}\n"
                f"다음 시도: {attempt + 1}/{MAX_RETRIES}"
            )

    # 9. 최대 재시도 초과
    print("최대 수정 횟수 초과", flush=True)

    send_message(
        f"🚨 Unity Harness 작업 중단\n"
        f"TODO: {task}\n"
        f"빌드 실패: {MAX_RETRIES}회\n"
        f"상태: 사용자 확인 필요\n"
        f"TODO 미완료 유지"
    )


if __name__ == "__main__":
    main()
