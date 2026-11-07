
import os
from pathlib import Path

from dotenv import load_dotenv
from slack_bolt import App
from slack_bolt.adapter.socket_mode import SocketModeHandler

from core.review_manager import ReviewManager


BASE_DIR = Path(__file__).resolve().parent

load_dotenv(BASE_DIR / ".env")

BOT_TOKEN = os.getenv("SLACK_BOT_TOKEN")
APP_TOKEN = os.getenv("SLACK_APP_TOKEN")
CHANNEL_ID = os.getenv("SLACK_CHANNEL_ID")
APPROVER_ID = os.getenv("SLACK_APPROVER_USER_ID")

if not BOT_TOKEN or not APP_TOKEN:
    raise RuntimeError("Slack 토큰이 설정되지 않았습니다.")

app = App(token=BOT_TOKEN)
review = ReviewManager(BASE_DIR)


def is_authorized(body):
    user_id = body.get("user", {}).get("id")
    channel_id = body.get("channel", {}).get("id")

    return (
        bool(APPROVER_ID)
        and user_id == APPROVER_ID
        and channel_id == CHANNEL_ID
    )


@app.action("harness_approve")
def handle_approve(ack, body, respond):
    ack()

    if not is_authorized(body):
        respond("승인 권한이 없습니다.")
        return

    task = body["actions"][0]["value"]

    if review.approve(task):
        respond(f"✅ 승인 완료: {task}")
    else:
        respond("승인할 대기 작업이 없습니다.")


@app.action("harness_reject")
def handle_reject(ack, body, respond):
    ack()

    if not is_authorized(body):
        respond("거절 권한이 없습니다.")
        return

    task = body["actions"][0]["value"]

    if review.reject(task):
        respond(f"❌ 거절 처리: {task}")
    else:
        respond("거절할 대기 작업이 없습니다.")


def main():
    print("=== Unity Harness Slack Listener ===")
    print("Slack Socket Mode 연결 시작...")

    handler = SocketModeHandler(app, APP_TOKEN)
    handler.start()


if __name__ == "__main__":
    main()
