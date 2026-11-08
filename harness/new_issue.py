"""
채팅 등으로 들어온 새 기능 요청을 GitHub 이슈로 등록하고 작업 브랜치를 만들어 올린다.

사용 예:
    python harness/new_issue.py "야간에 가로등 불빛 추가해줘"
    python harness/new_issue.py --file request.txt --todo

- 이슈 본문에 요청 원문을 그대로 기록한다(같은 요청이면 기존 이슈를 재사용).
- 작업 트리가 깨끗하면 feature/issue-<번호>-... 브랜치를 만들고 GitHub에 Push한다.
  커밋 안 된 다른 작업이 있으면 섞이지 않도록 브랜치 생성은 건너뛰고 이유를 출력한다.
- --todo를 주면 harness/TODO.md에 "- [ ] (#번호) 요청" 항목을 추가해 하네스가 이어서 처리하게 한다.
"""

import argparse
import sys
from pathlib import Path

from core.git_manager import GitManager
from core.github_issue_manager import GitHubIssueManager


BASE_DIR = Path(__file__).resolve().parent
PROJECT_DIR = BASE_DIR.parent


def add_todo(issue_number: int, request: str) -> bool:
    todo_path = BASE_DIR / "TODO.md"
    text = todo_path.read_text(encoding="utf-8")
    marker = f"(#{issue_number})"
    if marker in text:
        return False
    first_line = request.strip().splitlines()[0].strip()
    line = f"- [ ] {marker} {first_line}"
    if not text.endswith("\n"):
        text += "\n"
    todo_path.write_text(text + line + "\n", encoding="utf-8")
    return True


def main() -> int:
    parser = argparse.ArgumentParser(description="새 기능 요청 → GitHub 이슈 + 작업 브랜치")
    parser.add_argument("request", nargs="?", help="요청 원문")
    parser.add_argument("--file", help="요청 원문이 담긴 UTF-8 텍스트 파일")
    parser.add_argument("--source", default="채팅 요청 (Claude Code)", help="요청 출처")
    parser.add_argument("--todo", action="store_true", help="harness/TODO.md에 항목 추가")
    parser.add_argument("--no-branch", action="store_true", help="이슈만 만들고 브랜치는 만들지 않음")
    args = parser.parse_args()

    if args.file:
        request = Path(args.file).read_text(encoding="utf-8")
    elif args.request:
        request = args.request
    else:
        parser.error("요청 원문 또는 --file이 필요합니다.")
        return 2

    request = request.strip()
    if not request:
        parser.error("요청 원문이 비어 있습니다.")
        return 2

    git = GitManager(PROJECT_DIR)
    issues = GitHubIssueManager(PROJECT_DIR)

    issue = issues.ensure_issue(request, source=args.source)
    print(f"이슈: #{issue.number} {'(새로 생성)' if issue.created else '(기존 이슈 재사용)'} {issue.url}")

    if args.todo:
        added = add_todo(issue.number, request)
        print(f"TODO: {'추가함' if added else '이미 있음'}")

    if args.no_branch:
        print("브랜치: 건너뜀 (--no-branch)")
        return 0

    branch = git.branch_for_issue(issue.number, request)
    current = git.current_branch()

    if current != branch and git.has_changes():
        print(
            f"브랜치: 보류 — 현재 브랜치({current})에 커밋 안 된 변경이 있어 전환하지 않았습니다.\n"
            f"        변경을 커밋한 뒤 다시 실행하면 {branch} 브랜치를 만들어 올립니다."
        )
        return 0

    git.prepare_branch(branch)
    pushed = False
    if not git.remote_branch_exists(branch):
        git.push()
        pushed = True
    print(f"브랜치: {branch} ({'만들어 Push함' if pushed else '이미 원격에 있음'})")

    if issue.created or pushed:
        issues.comment(issue.number, f"🌿 작업 브랜치 `{branch}`를 만들어 Push했습니다.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
