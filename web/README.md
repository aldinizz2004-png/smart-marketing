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
