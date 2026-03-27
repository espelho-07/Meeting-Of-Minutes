# MOM Product UI Redesign

This folder is a React + Tailwind UI foundation for turning Meeting Of Minutes into a premium SaaS product surface.

## What it includes
- App shell with dark mode
- Modern sidebar and topbar
- Dashboard command surface
- Meetings list data table
- Meeting type configuration form
- Action Center workflow page
- Reusable buttons, badges, inputs, and surfaces

## Suggested folder structure
- src/components/layout: shell-level navigation and workspace chrome
- src/components/ui: reusable product primitives
- src/pages: page-level compositions
- src/lib: mock data and small UI helpers
- src/styles: global theme tokens and Tailwind layers

## Why this exists next to the MVC app
The current platform is ASP.NET MVC, but the product is now large enough that a true component-based frontend system is worth designing in parallel. This folder gives us a premium React/Tailwind direction without destabilizing the existing production app.

## Recommended migration path
1. Finalize design tokens and shell behavior here.
2. Port the shell and list system back into the MVC app.
3. Introduce API-backed page islands over time.
4. Move high-complexity workflow screens to React first:
   - Action Center
   - Dashboard
   - Meeting Details
   - Profile Requests

## Run locally
npm install
npm run dev
