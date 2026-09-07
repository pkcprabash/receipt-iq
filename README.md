# ReceiptIQ

Personal expense tracker: photo/upload a receipt, extract the details with
OCR, categorize the line items, and see spending trends over time.

## Vision

Paper and PDF receipts pile up and nobody reads them again until tax season
or a budgeting panic. ReceiptIQ turns a photo of a receipt into structured,
queryable data automatically:

1. **Capture** — upload or photograph a receipt.
2. **Extract** — Azure AI Document Intelligence pulls merchant, date, line
   items, and totals from the image.
3. **Categorize** — a rules engine (with an LLM fallback) assigns a category
   to each line item, not the receipt as a whole, since a single Target trip
   is groceries *and* household *and* maybe electronics.
4. **Understand** — dashboards show spend by category, by month, and by
   merchant, so trends are visible without spreadsheet archaeology.

The raw OCR response is kept forever alongside the parsed data, so
extraction and categorization can be improved and backfilled later without
asking anyone to re-upload a receipt.

## Stack

- **Backend:** .NET 9 Web API — `Api` / `Application` / `Domain` /
  `Infrastructure` projects
- **Database:** PostgreSQL + EF Core (`jsonb` for raw OCR payloads)
- **OCR:** Azure AI Document Intelligence (prebuilt receipt model), behind
  `IReceiptExtractor`
- **Frontend:** React + TypeScript + Vite, TanStack Query, Tailwind +
  shadcn/ui, Recharts
- **Local dev:** Docker Compose (Postgres + API + web)
- **CI/CD:** GitHub Actions → Azure Container Apps / Fly.io (API) +
  Cloudflare Pages (web)

## Repo layout

```
src/
  Api/             ASP.NET Core Web API (composition root)
  Application/     Use cases, interfaces (IReceiptExtractor, IFileStorage)
  Domain/          Entities, value objects — no external dependencies
  Infrastructure/  EF Core, Azure Document Intelligence, file storage
docs/
  PLAN.md          Day-by-day build plan
  decisions/       Architecture decision records
```

## Getting started
```bash
dotnet build ReceiptIQ.sln
```

Docker Compose for local Postgres + API arrives on Day 2.
