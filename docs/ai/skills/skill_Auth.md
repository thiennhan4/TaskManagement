# skill_Auth.md — Authorization & Permission Patterns

## Hierarchy

```
┌───────────────────────┐
│        Admin          │
│   (full access)       │
├───────────────────────┤
│      Member           │
│   (team member)       │
└──────────────┬────────┘
               │
         ┌─────┴─────┐
         │  Owner    │  (team owner)
         │  Manager  │  (team manager)
         └───────────┘
```

## Rules

When implementing authorization:

- First check if user is Admin
- If task is personal → only creator can modify
- If task is team:
    - Owner/Manager → full access
    - Member → only assigned tasks

Always fetch TeamMember to validate role