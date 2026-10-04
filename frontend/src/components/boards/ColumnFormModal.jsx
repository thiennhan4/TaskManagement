import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import Button from '@/components/ui/Button';

export default function ColumnFormModal({ title, form, setForm, onSubmit, onClose, loading, submitLabel, colors }) {
  return <Modal isOpen title={title} onClose={onClose} closeDisabled={loading} maxWidth="max-w-sm">
    <form onSubmit={onSubmit} className="p-6 space-y-5">
      <Input label="Column Name" type="text" autoFocus required maxLength={50} disabled={loading} value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} />
      {'quantity' in form && <Input label="Quantity" type="number" min={1} max={10} required disabled={loading} value={form.quantity || 1} onChange={event => setForm(current => ({ ...current, quantity: Math.max(1, Math.min(10, Number(event.target.value) || 1)) }))} />}
      <fieldset disabled={loading}><legend className="text-sm font-bold text-text-main mb-2">Column color</legend><div className="flex flex-wrap gap-2">{colors.map(color => <button key={color} type="button" aria-label={'Select color ' + color} aria-pressed={form.color === color} onClick={() => setForm(current => ({ ...current, color }))} className="w-8 h-8 rounded-lg border border-border-subtle focus-visible:outline-2 focus-visible:outline-text-main" style={{ backgroundColor: color, outline: form.color === color ? '2px solid var(--color-text-main)' : undefined }} />)}</div></fieldset>
      <div className="flex gap-3"><Button type="button" variant="outline" disabled={loading} onClick={onClose}>Cancel</Button><Button type="submit" isLoading={loading} disabled={!form.name.trim()}>{submitLabel}</Button></div>
    </form>
  </Modal>;
}
