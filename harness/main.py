
from pathlib import Path

from core.todo_manager import TodoManager
from core.coding_agent import CodingAgent
from slack_reporter import send_message


BASE_DIR = Path(__file__).resolve().parent
PROJECT_DIR = BASE_DIR.parent


def main():
    manager = TodoManager(BASE_DIR / "TODO.md")

    todo = manager.get_next_todo()

    if todo is None:
        print("모든 TODO 완료!")
        send_message("✅ Unity Harness: 모든 TODO 완료!")
        return

    print(f"현재 작업: {todo.description}")

    send_message(
        f"🚀 Codex 작업 시작\n"
        f"프로젝트: car3-c\n"
        f"TODO: {todo.description}"
    )

    agent = CodingAgent(PROJECT_DIR)

    try:
        success = agent.execute(todo.description)
    except Exception as e:
        print(f"Codex 실행 오류: {e}")
        send_message(f"❌ Codex 실행 오류: {e}")
        return

    if success:
        print("Codex 실행 성공")
        send_message(
            f"✅ Codex 실행 종료\n"
            f"TODO: {todo.description}\n"
            f"상태: 검증 대기"
        )
    else:
        print("Codex 실행 실패")
        send_message(
            f"❌ Codex 실행 실패\n"
            f"TODO: {todo.description}"
        )


if __name__ == "__main__":
    main()
