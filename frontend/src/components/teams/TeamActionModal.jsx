import { useState } from 'react';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import Select from '@/components/ui/Select';
import Textarea from '@/components/ui/Textarea';
import Button from '@/components/ui/Button';

export default function TeamActionModal({ title, fields, onSubmit, onClose, destructive = false }) {
  const [values, setValues] = useState(() => Object.fromEntries(fields.map(field => [field.name, field.value || ''])));
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const submit = async event => {
    event.preventDefault(); if (pending) return;
    const invalid = fields.find(field => field.required && !values[field.name].trim());
    if (invalid) { setError(invalid.label + ' is required.'); return; }
    setPending(true); setError('');
    try { await onSubmit(Object.fromEntries(Object.entries(values).map(([key, value]) => [key, value.trim()]))); onClose(); }
    catch (err) { setError(err.response?.data?.message || err.message || 'Action failed. Please retry.'); }
    finally { setPending(false); }
  };
  return <Modal isOpen title={title} onClose={onClose} closeDisabled={pending} maxWidth="max-w-lg"><form onSubmit={submit} className="p-6 space-y-5">
    {fields.map(field => {
      const props = { label: field.label, required: field.required, disabled: pending, value: values[field.name], onChange: event => setValues({ ...values, [field.name]: event.target.value }) };
      return field.options ? <Select key={field.name} {...props}>{field.options.map(value => <option key={value} value={value}>{value}</option>)}</Select> : field.type === 'textarea' ? <Textarea key={field.name} {...props} /> : <Input key={field.name} {...props} type={field.type || 'text'} />;
    })}
    {error && <p role="alert" className="text-danger">{error}</p>}
    <div className="flex gap-3"><Button variant="outline" type="button" disabled={pending} onClick={onClose}>Cancel</Button><Button type="submit" variant={destructive ? 'danger' : 'primary'} isLoading={pending}>Confirm</Button></div>
  </form></Modal>;
}
