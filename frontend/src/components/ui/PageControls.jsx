import Button from '@/components/ui/Button';

export default function PageControls({ page, loading, onPage }) {
  if (!page) return null;
  return <div className="flex flex-wrap items-center gap-3 py-3 text-sm text-text-muted" aria-live="polite">
    <span>{page.totalItems} items · Page {page.page} of {Math.max(1, page.totalPages)}</span>
    <Button disabled={loading || page.page <= 1} onClick={() => onPage(page.page - 1)}>Previous</Button>
    <Button disabled={loading || page.page >= page.totalPages} onClick={() => onPage(page.page + 1)}>Next</Button>
  </div>;
}
