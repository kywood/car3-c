
import os
from pathlib import Path

from dotenv import load_dotenv
from slack_sdk import WebClient
from slack_sdk.errors import SlackApiError


BASE_DIR = Path(__file__).resolve().parent

load_dotenv(BASE_DIR / ".env")


class SlackReporter:
    """Unity AI Harness Slack 보고 관리자."""

    def __init__(self):
        self.token = os.getenv("SLACK_BOT_TOKEN")
        self.channel = os.getenv("SLACK_CHANNEL_ID")

        if not self.token:
            raise RuntimeError(
                "SLACK_BOT_TOKEN 환경변수가 없습니다."
            )

        if not self.channel:
            raise RuntimeError(
                "SLACK_CHANNEL_ID 환경변수가 없습니다."
            )

        self.client = WebClient(token=self.token)

    def send_message(self, message: str) -> bool:
        """일반 메시지 전송."""
        try:
            response = self.client.chat_postMessage(
                channel=self.channel,
                text=message,
            )

            print(
                f"Slack 전송 성공: {response['ts']}",
                flush=True,
            )

            return bool(response["ok"])

        except SlackApiError as e:
            print(
                f"Slack 메시지 전송 실패: {e.response.data}",
                flush=True,
            )
            return False

    def send_screenshot(
        self,
        image_path: Path,
        comment: str,
    ) -> bool:
        """이미지 파일을 Slack에 업로드."""
        image_path = Path(image_path)

        if not image_path.is_file():
            print(f"스크린샷 파일 없음: {image_path}")
            return False

        try:
            response = self.client.files_upload_v2(
                channel=self.channel,
                file=str(image_path),
                title=image_path.name,
                initial_comment=comment,
            )

            print(
                "Slack 스크린샷 전송 성공",
                flush=True,
            )

            return bool(response["ok"])

        except SlackApiError as e:
            print(
                f"Slack 스크린샷 전송 실패: {e.response.data}",
                flush=True,
            )
            return False

    def send_review_request(self, task: str) -> bool:
        """Unity 빌드 성공 후 사용자 승인/거절 요청."""

        blocks = [
            {
                "type": "section",
                "text": {
                    "type": "mrkdwn",
                    "text": (
                        "✅ *Unity Android 빌드 성공*\n"
                        "📋 *게임 동작 검증 요청*\n"
                        f"*TODO:* {task}\n\n"
                        "게임을 실행하여 기능을 확인한 뒤 "
                        "승인하거나 거절해 주세요."
                    ),
                },
            },
            {
                "type": "actions",
                "elements": [
                    {
                        "type": "button",
                        "text": {
                            "type": "plain_text",
                            "text": "✅ 승인",
                            "emoji": True,
                        },
                        "style": "primary",
                        "action_id": "harness_approve",
                        "value": task,
                    },
                    {
                        "type": "button",
                        "text": {
                            "type": "plain_text",
                            "text": "❌ 거절",
                            "emoji": True,
                        },
                        "style": "danger",
                        "action_id": "harness_reject",
                        "value": task,
                    },
                ],
            },
        ]

        try:
            response = self.client.chat_postMessage(
                channel=self.channel,
                text=f"Unity Harness 검증 요청: {task}",
                blocks=blocks,
            )

            print(
                f"Slack 승인 요청 전송 성공: {response['ts']}",
                flush=True,
            )

            return bool(response["ok"])

        except SlackApiError as e:
            print(
                f"Slack 승인 요청 전송 실패: {e.response.data}",
                flush=True,
            )
            return False


# 기존 main.py와의 호환성을 위한 함수
_reporter = None


def get_reporter() -> SlackReporter:
    global _reporter

    if _reporter is None:
        _reporter = SlackReporter()

    return _reporter


def send_message(message: str) -> bool:
    return get_reporter().send_message(message)


def send_screenshot(
    image_path: Path,
    comment: str,
) -> bool:
    return get_reporter().send_screenshot(
        image_path,
        comment,
    )


def send_review_request(task: str) -> bool:
    return get_reporter().send_review_request(task)
