
import os

from dotenv import load_dotenv
from slack_sdk import WebClient
from slack_sdk.errors import SlackApiError

from pathlib import Path


load_dotenv()


def send_message(message: str) -> bool:
    token = os.getenv("SLACK_BOT_TOKEN")
    channel = os.getenv("SLACK_CHANNEL_ID")

    if not token or not channel:
        print("Slack 환경변수가 설정되지 않았습니다.")
        return False

    client = WebClient(token=token)

    try:
        response = client.chat_postMessage(
            channel=channel,
            text=message,
        )
        print(f"Slack 전송 성공: {response['ts']}")
        return True

    except SlackApiError as e:
        print(f"Slack 전송 실패: {e.response['error']}")
        return False

def send_screenshot(image_path: Path, comment: str) -> bool:
    token = os.getenv("SLACK_BOT_TOKEN")
    channel = os.getenv("SLACK_CHANNEL_ID")

    if not token or not channel:
        print("Slack 환경변수가 설정되지 않았습니다.")
        return False

    client = WebClient(token=token)

    try:
        response = client.files_upload_v2(
            channel=channel,
            file=str(image_path),
            title=image_path.name,
            initial_comment=comment,
        )
        print("Slack 스크린샷 전송 성공")
        return bool(response["ok"])


    except SlackApiError as e:

        print(f"Slack 스크린샷 전송 실패: {e.response.data}")

        return False
