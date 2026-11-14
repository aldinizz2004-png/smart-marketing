# Smart Marketing – Web

The web app for shop owners: React 19, TypeScript, Vite, Tailwind CSS v4, TanStack Query, React Router and i18next.
Arabic (RTL) is the default language, English is optional, and light and dark themes are supported.

## Run locally

1. Start the API (it allows `http://localhost:5233` to be called from `http://localhost:5173` in Development):

   ```powershell
   $env:Ai__Provider='Fake'; dotnet run --project SmartMarketingPlatform --launch-profile http
   ```

2. Start the web app:

   ```powershell
   cd web
   npm install
   npm run dev
   ```

   Open http://localhost:5173. The dot at the bottom of the sidebar shows whether the API is reachable.

To use another API address, copy `.env.example` to `.env.local` and change `VITE_API_URL`.

## Landing page (Week 1 task)

The public landing page lives at `/welcome` and is built from the same components, theme and translations as the app (`src/pages/landing/`).

- A first-time visitor who opens `/` is sent to `/welcome`; someone who has signed in before goes to `/login`, and a signed-in user sees a link to the dashboard instead of the sign-in buttons.
- Sections: a fixed header with a mobile menu, the hero with the main call to action, a three-column features grid, the tech stack badges, and a demo request form.
- The demo form uses `react-hook-form` with `zod` validation. There is no endpoint for demo requests yet, so the request is kept in `localStorage` (`smp.demoRequests`) and a confirmation is shown.
- "شوف بنية النظام" opens a modal with the system architecture. Dialogs move focus inside, keep Tab inside, close with Escape and give focus back to the button that opened them.
- Semantic markup (`header`, `nav`, `main`, `section`, `form`, `footer`), one `h1`, labelled fields with error messages linked by `aria-describedby`, and no horizontal scroll from phone to desktop width.

To see it while signed in, open http://localhost:5173/welcome directly.

## Pages

| Route | What it does |
| --- | --- |
| `/welcome` | Public landing page |
| `/login`, `/register`, `/onboarding` | Sign in, create an account, then plan, shop, brand and accounts |
| `/` | Home: counts, upcoming posts and the latest report |
| `/content`, `/campaigns` | Write, review and schedule posts; campaigns with AI plans |
| `/inbox`, `/comments` | WhatsApp conversations and comments with suggested replies |
| `/catalog` | Products, offers and the media library |
| `/competitors` | Tracked accounts, scans and the insights drawn from them |
| `/insights` | Performance numbers, AI recommendations and strategy runs |
| `/reports` | Owner reports, their email version, and sending them |
| `/settings` | Shop details and brand, social accounts, automation policy, team, subscription and AI usage |

What each role can do follows the API policies (`src/auth/roles.ts`): viewers read, editors write, reviewers approve, admins manage the shop and team, and only the owner changes the subscription.

## Scripts

| Script | What it does |
| --- | --- |
| `npm run dev` | Development server on port 5173 |
| `npm run build` | Type check and production build into `dist/` |
| `npm run lint` | ESLint |
| `npm run typecheck` | TypeScript only |
| `npm test` | Vitest (jsdom + Testing Library) |
| `npm run api:types` | Regenerates `src/api/schema.d.ts` from the running API's Swagger document |

Run `npm run api:types` after every API change and commit the result, so the typed client in `src/api/client.ts` matches the API.

## Structure

```
src/
  api/          typed API client (openapi-fetch) and generated schema
  app/          providers, routes and the layout (sidebar, top bar, bottom navigation)
  components/   shared components; ui/ holds the basic building blocks
  i18n/         i18next setup and the ar/en translations
  lib/          small helpers (class names, theme)
  pages/        one file per page
  styles/       Tailwind theme: brand colors, surfaces and dark mode
```
