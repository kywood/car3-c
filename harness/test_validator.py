
from pathlib import Path

from core.validator import UnityValidator


BASE_DIR = Path(__file__).resolve().parent
PROJECT_DIR = BASE_DIR.parent


def main():
    print("=== Unity Validator Test ===")

    validator = UnityValidator(PROJECT_DIR)
    result = validator.validate()

    print(f"빌드 성공: {result.success}")
    print(f"종료 코드: {result.returncode}")
    print(f"로그 경로: {result.log_path}")

    if not result.success:
        print("빌드 실패 원인:")
        print(result.output[-3000:])


if __name__ == "__main__":
    main()
