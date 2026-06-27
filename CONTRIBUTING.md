# Contributing

## Commit style

Use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` a new feature
- `fix:` a bug fix
- `chore:` tooling/maintenance
- `docs:` documentation only
- `infra:` infrastructure / IaC
- `refactor:`, `test:`, `perf:` as needed

Example: `feat(api): add /api/lists/current endpoint`

## Branching

- Default branch: `main` (protected — PR + passing CI required).
- Work on short-lived feature branches; open a PR; SWA publishes a free preview environment.

## Local checks before pushing

```bash
npm run lint
npm run typecheck
npm run test
npm run build
dotnet test api
```

## Code style

- Web: ESLint + Prettier, 2-space indent, LF.
- C#: `dotnet format`, 4-space indent.
- `.editorconfig` is the source of truth for whitespace.
