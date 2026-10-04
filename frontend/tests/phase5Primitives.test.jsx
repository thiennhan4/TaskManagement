import Card from '@/components/ui/Card';
import { StrictMode } from 'react';
import { afterEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import Select from '@/components/ui/Select';
import Textarea from '@/components/ui/Textarea';
import Button from '@/components/ui/Button';
import { safeReturnLocation } from '@/utils/returnLocation';
import { TASK_STATUSES, TASK_PRIORITIES } from '@/constants/taskStatus';

afterEach(cleanup);
for (const [name, Component] of [['Input', Input], ['Select', Select], ['Textarea', Textarea]]) {
  it(name + ' associates stable labels, errors and descriptions', () => {
    const { rerender } = render(<Component label="Field" error="Invalid" required aria-describedby="hint" />);
    const control = screen.getByLabelText('Field'); const id = control.id;
    expect(control.getAttribute('aria-invalid')).toBe('true');
    expect(control.getAttribute('aria-describedby')).toBe('hint ' + id + '-error');
    expect(document.getElementById(id + '-error').textContent).toBe('Invalid');
    expect(control.required).toBe(true);
    rerender(<Component label="Field" disabled />);
    expect(screen.getByLabelText('Field').id).toBe(id);
    expect(screen.getByLabelText('Field').disabled).toBe(true);
    expect(screen.getByLabelText('Field').getAttribute('aria-describedby')).toBeNull();
  });
}

it('provides modal semantics, focus entry/trap/Escape/restore and preserves scroll in StrictMode', () => {
  const trigger = document.createElement('button'); document.body.appendChild(trigger); trigger.focus();
  document.body.style.overflow = 'auto'; const close = vi.fn();
  const { rerender, unmount } = render(<StrictMode><Modal isOpen title="Edit profile" description="Account details" onClose={close}><Input label="Name" /><Button>Save</Button></Modal></StrictMode>);
  const dialog = screen.getByRole('dialog', { name: 'Edit profile' });
  expect(dialog.getAttribute('aria-modal')).toBe('true');
  expect(document.getElementById(dialog.getAttribute('aria-describedby')).textContent).toBe('Account details');
  const first = screen.getByRole('button', { name: 'Close dialog' }); const last = screen.getByRole('button', { name: 'Save' });
  expect(document.activeElement).toBe(first);
  last.focus(); fireEvent.keyDown(document, { key: 'Tab' }); expect(document.activeElement).toBe(first);
  fireEvent.keyDown(document, { key: 'Tab', shiftKey: true }); expect(document.activeElement).toBe(last);
  fireEvent.keyDown(document, { key: 'Escape' }); expect(close).toHaveBeenCalledOnce();
  rerender(<StrictMode><Modal isOpen={false} title="Edit profile" onClose={close} /></StrictMode>);
  expect(document.activeElement).toBe(trigger); expect(document.body.style.overflow).toBe('auto');
  unmount(); trigger.remove(); document.body.style.overflow = '';
});

it('keeps nested modal focus and scroll ownership independent', () => {
  const close = vi.fn();
  const { rerender } = render(<Modal isOpen title="Parent" onClose={close}><Button>Open</Button><Modal isOpen title="Child" onClose={close}><Button>Child save</Button></Modal></Modal>);
  expect(screen.getByRole('dialog', { name: 'Child' }).contains(document.activeElement)).toBe(true);
  fireEvent.keyDown(document, { key: 'Escape' }); expect(close).toHaveBeenCalledOnce();
  rerender(<Modal isOpen title="Parent" onClose={close}><Button>Open</Button></Modal>);
  expect(document.body.style.overflow).toBe('hidden');
  expect(screen.getByRole('dialog', { name: 'Parent' }).contains(document.activeElement)).toBe(true);
});

it('prevents pending modal dismissal and loading button overrides', () => {
  const close = vi.fn(); render(<Modal isOpen title="Deleting" closeDisabled onClose={close}><Button isLoading disabled={false}>Delete</Button></Modal>);
  fireEvent.keyDown(document, { key: 'Escape' }); expect(close).not.toHaveBeenCalled();
  expect(screen.getByRole('button', { name: 'Close dialog' }).disabled).toBe(true);
  expect(screen.getByRole('button', { name: 'Delete' }).disabled).toBe(true);
});

it('accepts only validated internal return locations', () => {
  expect(safeReturnLocation({ pathname: '/tasks/123', search: '?tab=time', hash: '#detail' })).toBe('/tasks/123?tab=time#detail');
  for (const value of ['https://evil.invalid', '//evil.invalid', '/\\evil.invalid', '/%2fevil.invalid', '/%5cevil.invalid', '/%0aevil.invalid', '/login', 'javascript:alert(1)', '/%zz']) expect(safeReturnLocation(value)).toBe('/dashboard');
});

it('matches backend status/priority values', () => {
  expect(TASK_STATUSES).toEqual(['Todo', 'InProgress', 'Review', 'Done']);
  expect(TASK_PRIORITIES).toEqual(['Low', 'Medium', 'High', 'Critical']);
  expect(TASK_STATUSES).not.toContain('Blocked');
});

it('Card forwards interaction and visual props used by project cards', () => {
  const click = vi.fn();
  render(<Card onClick={click} data-testid="project-card" style={{ borderLeftColor: 'var(--color-primary)' }}>Project</Card>);
  fireEvent.click(screen.getByTestId('project-card'));
  expect(click).toHaveBeenCalledOnce();
  expect(screen.getByTestId('project-card').style.borderLeftColor).toContain('var(--color-primary)');
});
