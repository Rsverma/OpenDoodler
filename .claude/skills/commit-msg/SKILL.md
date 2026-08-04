---
name: commit-msg
description: Generate a conventional commit message from staged (or currently modified) changes and commit them. Use when the user says "write a commit message", "generate a commit", "commit changes", or asks to commit without dictating the exact message.
---

# commit-msg

Generate a Conventional Commits-style message from the actual diff and commit it — never invent a
message before looking at the diff, and never commit changes the user hasn't implicitly asked to save.

## Steps

1. **Check for staged changes.**
   ```bash
   git diff --staged
   ```

2. **If nothing is staged, stage relevant changes.**
   - Run `git status` to see what's modified/untracked.
   - Stage files that look like intentional work-in-progress for the current task (source files,
     config, docs the user was clearly editing).
   - Do **not** stage build output, `bin/`/`obj/`, `.vs/`, local databases, or anything that looks like
     a secret or credential (`.env`, `*.pfx`, connection strings with passwords, etc.) — leave those
     untracked and flag them to the user instead of silently including them.
   - Stage specific files by name (`git add <path> <path> ...`). Do not use `git add -A` or `git add .`
     — a broad add can sweep in files the user didn't intend to commit.
   - After staging, re-run `git diff --staged` to confirm what will actually be committed.

3. **Generate a Conventional Commit message from the staged diff.**
   - **Type** — pick the one that best matches the dominant change:
     - `feat` — new user-facing capability
     - `fix` — bug fix
     - `refactor` — code restructuring with no behavior change
     - `chore` — tooling, deps, build config, non-source housekeeping
     - `docs` — documentation only (README, CLAUDE.md, comments-only changes)
     - `test` — test-only changes
   - **Scope** — pick the one that best matches which part of the codebase changed (adapt to what the
     diff actually touches; these are examples, not a fixed enum):
     - `data` — `OpenBoardAnim.Library` (DataContext, Entities, Migrations, Repositories)
     - `config` — `.csproj`/`.sln` files, `App.xaml.cs` DI wiring, settings/build config
     - `ui` — `OpenBoardAnim/Views`, `ViewModels`, `Controls`, XAML, `Themes`
     - `api` — `OpenBoardAnim.Utilities`, `OpenBoardAnim.VideoTools`, or other shared/service-layer code
     - Omit the scope entirely if the change spans multiple areas with no single dominant one.
   - **Subject** — one short imperative-mood line (e.g. "fix migrations not re-running on startup"),
     no trailing period, under ~72 characters total including `type(scope): `.
   - **Body** — bullet points (`- `) describing *what* changed and *why*, derived from the diff and any
     context available in the conversation (not invented). Omit the body for genuinely trivial
     single-line changes where the subject already says it all.
   - Format:
     ```
     <type>(<scope>): <subject>

     - <what changed and why>
     - <what changed and why>
     ```

4. **Commit with the generated message.**
   ```bash
   git commit -m "$(cat <<'EOF'
   <type>(<scope>): <subject>

   - <bullet>
   - <bullet>
   EOF
   )"
   ```
   Use a heredoc (as above) rather than `-m "..." -m "..."` so multi-line bodies are formatted
   correctly. Do not add a `Co-Authored-By` trailer unless the user's global/project conventions
   already call for one.

5. **Confirm.** Run `git status` after committing to show the result, and surface the final commit
   message back to the user.

## Notes

- If the staged diff mixes clearly unrelated concerns (e.g. an unrelated formatting pass plus a real
  fix), mention this to the user rather than silently picking one type/scope to represent everything.
- If `git diff --staged` is still empty after attempting to stage (e.g. truly no changes exist), stop
  and tell the user there's nothing to commit — do not fabricate a commit.
