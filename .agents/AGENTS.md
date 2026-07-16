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

## State Management Guidelines

### 1. TanStack Query (React Query)
- **Separation of Concerns**: Do not call `useQuery` or `useMutation` directly in UI components. Wrap them in custom hooks within the feature's `hooks` folder.
- **Query Keys**: Use consistent query keys or query key factories to avoid errors and ease cache invalidation.
- **Status handling**: Pass down status variables (`isLoading`, `isError`) or pre-processed data to presentation components.

### 2. Zustand
- **Global State**: Use Zustand for global UI or client-side sync state (e.g., user session, theme, global modals, persistent filters).
- **Selectors**: Always use selectors when retrieving state to prevent unnecessary re-renders.
- **Encapsulated Actions**: Define action functions inside the store alongside the state.

### 3. React Context
- **Low-frequency State**: Use React Context only for low-frequency state updates or local component-tree level configuration (e.g., localized form context, custom table wrapper contexts).
- **Prevent Re-renders**: Avoid using Context for global, high-frequency updates.
- **Custom Hook wrapper**: Wrap Context consumption in a custom hook that throws a clear error if used outside its Provider.
