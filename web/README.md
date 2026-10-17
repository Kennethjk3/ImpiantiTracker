# Impianti web app

The browser front end for ImpiantiTracker, built with **SvelteKit 3**, **Svelte 5** and **TypeScript**.

It has four pages:

- **Plan:** what's overdue or due soon, with a "thermal strip" that plots every installation by due date.
- **Installations:** a filterable list.
- **Installation detail:** service history and a form for recording a visit.
- **Report:** a yearly report.

The app never talks to the database. Its server-side code calls the ASP.NET Core API in `src/ImpiantiTracker.Api`. The browser only ever talks to SvelteKit, so the API needs no CORS setup and its address stays private.

## Run it

You need **Node.js 22.17 or newer** (`node --version`). Start the API first, from the repository root:

```sh
dotnet run --project src/ImpiantiTracker.Api
```

Then start the web app from this folder:

```sh
npm ci
npm run dev
```

Open http://localhost:5173.

If the API runs somewhere other than `http://localhost:5080`, set `API_URL` (for example in a `.env` file in this folder: `API_URL=http://localhost:5080`).

## Scripts

| Command | What it does |
| --- | --- |
| `npm run dev` | Development server with hot reload |
| `npm run check` | Type-check TypeScript and Svelte |
| `npm test` | Unit tests (Vitest) |
| `npm run build` | Production build into `build/` |
| `npm run preview` | Serve the production build on http://localhost:4173 |

## Deploying

`npm run build` produces a standalone Node server: run it with `node build`. It expects to sit behind an HTTPS reverse proxy. Without `paths.origin` configured, it assumes `https`, so if you open it over plain `http://` form submissions are rejected as cross-site. To try the production build locally, use `npm run preview`.

## How it's built

- **Server-side data loading** (`+page.server.ts`): every page loads its data on the server, so it renders without JavaScript.
- **A form action** records visits. It works as a plain HTML form, and `use:enhance` makes it update without a page reload when JavaScript is on. The API's validation messages are shown next to the form.
- **`src/lib/server/api.ts`** is the only place that calls the API. It turns a down API into a clear 503 page and a missing installation into a 404.
- **Dates stay `yyyy-mm-dd` strings** end to end and are formatted without `Date` objects, so no time zone can shift a day.
- **Phone layout:** tables turn into stacked cards on a phone, and phone numbers are tap-to-call links.
- **Accessibility:** status is shown by shape and text as well as colour, the strip's markers are keyboard-reachable links with labels, there's a skip link, and both light and dark themes meet WCAG AA contrast.
