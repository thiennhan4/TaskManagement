import { useState } from 'react';
import { useAuth } from '@/context/authState';
import { settingsApi } from '@/api/settingsApi';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Card from '@/components/ui/Card';
import toast from 'react-hot-toast';

export default function ProfilePage() {
  const { user, refreshProfile } = useAuth();
  const [tab, setTab] = useState('profile');
  return <div className="max-w-4xl mx-auto space-y-6">
    <div><h1 className="text-3xl font-bold text-text-main">Settings</h1><p className="text-text-muted">Manage your profile and account security.</p></div>
    <nav aria-label="Settings sections" className="flex flex-wrap gap-2">{['profile', 'security', 'notifications'].map(name => <Button key={name} variant={tab === name ? 'primary' : 'outline'} aria-pressed={tab === name} onClick={() => setTab(name)}>{name === 'security' ? 'Password & Security' : name === 'profile' ? 'Profile' : 'Notifications'}</Button>)}</nav>
    <Card hover={false}>
      {tab === 'profile' && <ProfileForm key={user?.id} user={user} refreshProfile={refreshProfile} />}
      {tab === 'security' && <PasswordForm />}
      {tab === 'notifications' && <p>Notification preferences are unavailable until persisted preferences are supported. No preference changes can be saved here.</p>}
    </Card>
  </div>;
}

function ProfileForm({ user, refreshProfile }) {
  const [name, setName] = useState(user?.fullName || '');
  const [avatar, setAvatar] = useState(user?.avatarUrl || '');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const submit = async event => {
    event.preventDefault();
    if (pending) return;
    if (name.trim().length < 2) { setError('Full name must contain at least 2 characters.'); return; }
    setPending(true); setError('');
    let saved = false;
    try {
      const { data } = await settingsApi.updateProfile({ fullName: name.trim(), avatarUrl: avatar.trim() || null });
      if (!data.success) throw new Error(data.message || 'Profile could not be saved.');
      saved = true;
      await refreshProfile();
      toast.success('Profile updated successfully!');
    } catch (err) {
      setError(saved ? 'Profile saved, but current-user refresh failed. Reload to see your saved profile.' : err.response?.data?.message || err.message || 'Profile could not be saved.');
    } finally { setPending(false); }
  };
  return <form onSubmit={submit} className="space-y-5">
    <h2 className="text-xl font-bold">Profile</h2>
    <Input label="Full name" required minLength={2} value={name} disabled={pending} onChange={e => setName(e.target.value)} />
    <Input label="Avatar URL" type="url" value={avatar} disabled={pending} onChange={e => setAvatar(e.target.value)} />
    <Input label="Email" type="email" value={user?.email || ''} disabled />
    {error && <p role="alert" className="text-danger">{error}</p>}
    <Button type="submit" isLoading={pending}>Save profile</Button>
  </form>;
}

function PasswordForm() {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmation: '' });
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const submit = async event => {
    event.preventDefault();
    if (pending) return;
    if (!form.currentPassword || form.newPassword.length < 6 || form.newPassword !== form.confirmation) {
      setError('Enter your current password, a new password of at least 6 characters, and matching confirmation.'); return;
    }
    setPending(true); setError('');
    try {
      const { data } = await settingsApi.changePassword({ currentPassword: form.currentPassword, newPassword: form.newPassword });
      if (!data.success) throw new Error(data.message || 'Password could not be changed.');
      setForm({ currentPassword: '', newPassword: '', confirmation: '' });
      toast.success('Password changed. Sessions cannot refresh; sign in again when your current session expires.');
    } catch (err) { setError(err.response?.data?.message || err.message || 'Password could not be changed.'); }
    finally { setPending(false); }
  };
  return <form onSubmit={submit} className="space-y-5">
    <h2 className="text-xl font-bold">Password & Security</h2>
    <p className="text-text-muted">Minimum 6 characters. Changing your password revokes refresh sessions on all devices. Existing access sessions expire normally.</p>
    {Object.entries({ currentPassword: 'Current password', newPassword: 'New password', confirmation: 'Confirm new password' }).map(([field, label]) => <Input key={field} label={label} type="password" autoComplete={field === 'currentPassword' ? 'current-password' : 'new-password'} required disabled={pending} value={form[field]} onChange={e => setForm({ ...form, [field]: e.target.value })} />)}
    {error && <p role="alert" className="text-danger">{error}</p>}
    <Button type="submit" isLoading={pending}>Update password</Button>
  </form>;
}
