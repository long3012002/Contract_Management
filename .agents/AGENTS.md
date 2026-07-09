# Project Rules

## Architecture & Code Organization
- **Feature-based Structure**: Code should be organized by feature in the `frontend/src/features` directory. Each feature should contain its own `components`, `hooks`, `utils`, and `api` if applicable.
- **Separation of Concerns (Logic vs UI)**: Strictly separate business/state logic from UI components.
  - UI components (Presentational Components) should primarily accept props and emit events.
  - Logic (Container Components or Custom Hooks) should handle data fetching, state management, and side effects.
- **Component Reusability**: Break down large UI components into smaller, reusable pieces. Place generic reusable components in `frontend/src/components/ui` or `frontend/src/components/common`.

## Styling
- Use Tailwind CSS v4 alongside shadcn/ui.
- Follow the monochrome and brand accent design system established in `docs/system_design.md`.
