"""GitHub 이슈/브랜치 규칙의 순수 함수 테스트(네트워크 없음). 실행: python -m unittest test_github_issue"""

import unittest
from pathlib import Path

from core.git_manager import GitManager
from core.github_issue_manager import (
    TASK_MARKER,
    issue_body,
    issue_number_in,
    issue_title,
    repo_from_url,
    task_key,
)


class GitHubIssueRulesTest(unittest.TestCase):
    def test_repo_from_https_and_ssh(self):
        self.assertEqual(repo_from_url("https://github.com/kywood/car3-c.git"), "kywood/car3-c")
        self.assertEqual(repo_from_url("git@github.com:kywood/car3-c.git"), "kywood/car3-c")

    def test_issue_number_in_todo(self):
        self.assertEqual(issue_number_in("(#12) 야간 가로등"), 12)
        self.assertIsNone(issue_number_in("야간 가로등"))

    def test_task_key_ignores_issue_reference(self):
        self.assertEqual(task_key("(#12) 야간 가로등"), task_key("야간 가로등"))
        self.assertNotEqual(task_key("야간 가로등"), task_key("주간 가로등"))

    def test_body_keeps_request_verbatim_and_marker(self):
        request = "야간에 가로등 불빛 넣고\n차 헤드라이트 빛 더 밝고 넓게"
        body = issue_body(request, "채팅 요청", task_key(request))
        self.assertIn("> 야간에 가로등 불빛 넣고", body)
        self.assertIn("> 차 헤드라이트 빛 더 밝고 넓게", body)
        self.assertIn(f"{TASK_MARKER}:{task_key(request)}", body)

    def test_title_is_short_first_line(self):
        title = issue_title("가" * 100 + "\n둘째 줄")
        self.assertTrue(title.startswith("[기능] "))
        self.assertLessEqual(len(title), 6 + 61)
        self.assertNotIn("둘째", title)

    def test_branch_name_contains_issue_number(self):
        git = GitManager(Path("."))
        self.assertEqual(git.branch_for_issue(12, "(#12) Add rocket launch"), "feature/issue-12-add-rocket-launch")
        self.assertEqual(git.branch_for_issue(7, "야간 가로등"), "feature/issue-7")


if __name__ == "__main__":
    unittest.main()
