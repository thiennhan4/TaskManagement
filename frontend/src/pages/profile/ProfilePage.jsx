import React, { useState } from 'react';
import { useAuth } from '@/context/authState';
import { 
  User, 
  Mail, 
  Lock, 
  Bell, 
  Shield, 
  Camera, 
  Save, 
  LogOut,
  ChevronRight,
  Globe
} from 'lucide-react';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import toast from 'react-hot-toast';

export default function ProfilePage() {
  const { user, logout } = useAuth();
  const [activeTab, setActiveTab] = useState('profile');
  const [isLoading, setIsLoading] = useState(false);

  // Form states
  const [formData, setFormData] = useState({
    fullName: user?.fullName || '',
    email: user?.email || '',
    bio: 'Product Designer & Coffee enthusiast. Building the future of task management at TaskHub.',
    jobTitle: 'Senior Product Manager',
    location: 'San Francisco, CA',
  });

  const handleUpdateProfile = (e) => {
    e.preventDefault();
    setIsLoading(true);
    // Mock update
    setTimeout(() => {
      setIsLoading(false);
      toast.success('Profile updated successfully!');
    }, 1000);
  };

  const tabs = [
    { id: 'profile', label: 'Public Profile', icon: User },
    { id: 'account', label: 'Account Settings', icon: Globe },
    { id: 'security', label: 'Password & Security', icon: Shield },
    { id: 'notifications', label: 'Notifications', icon: Bell },
  ];

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-black text-text-main tracking-tight">
          Settings
        </h1>
        <p className="text-text-muted mt-1 font-medium">
          Manage your account settings and profile preferences.
        </p>
      </div>

      <div className="flex flex-col lg:flex-row gap-8">
        {/* Sidebar Navigation */}
        <aside className="lg:w-64 flex-shrink-0">
          <nav className="space-y-1">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`
                  w-full flex items-center justify-between px-4 py-3 rounded-xl transition-all duration-200
                  ${activeTab === tab.id 
                    ? 'bg-primary text-white shadow-lg shadow-primary/20 font-bold' 
                    : 'text-text-muted hover:bg-hover-bg font-medium'}
                `}
              >
                <div className="flex items-center gap-3">
                  <tab.icon size={18} />
                  <span>{tab.label}</span>
                </div>
                {activeTab === tab.id && <ChevronRight size={14} />}
              </button>
            ))}
          </nav>

          <div className="mt-8 pt-8 border-t border-border-subtle">
            <button 
              onClick={logout}
              className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-950/20 font-bold transition-all"
            >
              <LogOut size={18} />
              <span>Logout</span>
            </button>
          </div>
        </aside>

        {/* Content Area */}
        <div className="flex-1 space-y-6">
          {activeTab === 'profile' && (
            <Card className="animate-fade-in" padding="p-0 overflow-hidden">
              <div className="px-8 py-6 border-b border-border-subtle bg-surface-1/50">
                <h3 className="text-xl font-bold text-text-main">Public Profile</h3>
                <p className="text-sm text-text-muted font-medium mt-1">This information will be displayed publicly.</p>
              </div>

              <form onSubmit={handleUpdateProfile} className="p-8 space-y-8">
                {/* Avatar Upload */}
                <div className="flex flex-col sm:flex-row items-center gap-8 pb-8 border-b border-border-subtle">
                  <div className="relative group">
                    <div className="w-24 h-24 rounded-3xl bg-primary/10 flex items-center justify-center text-primary text-3xl font-black border-2 border-white shadow-premium overflow-hidden">
                      {user?.avatar ? (
                        <img src={user.avatar} alt="Avatar" className="w-full h-full object-cover" />
                      ) : (
                        user?.fullName?.charAt(0) || 'U'
                      )}
                    </div>
                    <button className="absolute -bottom-2 -right-2 p-2 bg-surface-0 rounded-xl shadow-premium border border-border-subtle text-text-muted hover:text-primary transition-colors">
                      <Camera size={16} />
                    </button>
                  </div>
                  <div className="text-center sm:text-left">
                    <h4 className="font-bold text-text-main">Profile Photo</h4>
                    <p className="text-xs text-text-muted mt-1 font-medium max-w-[200px]">
                      PNG, JPG or GIF. Max 2MB. Recommended size 400x400.
                    </p>
                    <div className="flex gap-3 mt-4">
                      <Button variant="outline" size="sm">Change Photo</Button>
                      <Button variant="ghost" size="sm" className="text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-950/20">Remove</Button>
                    </div>
                  </div>
                </div>

                {/* Form Fields */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <Input 
                    label="Full Name" 
                    value={formData.fullName} 
                    onChange={(e) => setFormData({...formData, fullName: e.target.value})}
                  />
                  <Input 
                    label="Job Title" 
                    value={formData.jobTitle} 
                    onChange={(e) => setFormData({...formData, jobTitle: e.target.value})}
                  />
                  <div className="md:col-span-2 flex flex-col gap-1.5">
                    <label className="text-sm font-bold text-text-main ml-1">Biography</label>
                    <textarea 
                      className="w-full px-4 py-2.5 rounded-xl border border-border-subtle bg-surface-0 text-text-main placeholder:text-text-muted focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all duration-200 min-h-[120px]"
                      value={formData.bio}
                      onChange={(e) => setFormData({...formData, bio: e.target.value})}
                    />
                  </div>
                  <Input 
                    label="Location" 
                    value={formData.location} 
                    onChange={(e) => setFormData({...formData, location: e.target.value})}
                  />
                  <div className="md:col-span-2 flex flex-col gap-1.5">
                    <label className="text-sm font-bold text-text-main ml-1">Connected Accounts</label>
                    <div className="flex gap-4 mt-2">
                      <button className="p-3 rounded-xl border border-border-subtle hover:bg-hover-bg transition-all text-text-main">
                        <Globe size={20} />
                      </button>
                      <button className="p-3 rounded-xl border border-border-subtle hover:bg-hover-bg transition-all text-blue-400">
                        <Globe size={20} />
                      </button>
                    </div>
                  </div>
                </div>

                <div className="flex justify-end pt-4 border-t border-border-subtle">
                  <Button 
                    type="submit" 
                    leftIcon={<Save size={18} />}
                    isLoading={isLoading}
                  >
                    Save Changes
                  </Button>
                </div>
              </form>
            </Card>
          )}

          {activeTab === 'account' && (
            <Card className="animate-fade-in p-8 text-center">
              <div className="w-16 h-16 bg-surface-1 rounded-full flex items-center justify-center mx-auto mb-4 text-text-subtle">
                <Globe size={32} />
              </div>
              <h3 className="text-lg font-bold text-text-main">Account Settings</h3>
              <p className="text-text-muted max-w-sm mx-auto mt-2">Manage your email preferences and international settings.</p>
              <div className="mt-8 space-y-4 text-left max-w-md mx-auto">
                <Input label="Email Address" value={formData.email} disabled />
                <p className="text-xs text-text-muted px-1 italic">Contact support to change your verified email address.</p>
              </div>
            </Card>
          )}

          {activeTab === 'security' && (
            <Card className="animate-fade-in" padding="p-0 overflow-hidden">
               <div className="px-8 py-6 border-b border-border-subtle bg-surface-1/50">
                <h3 className="text-xl font-bold text-text-main">Security Settings</h3>
                <p className="text-sm text-text-muted font-medium mt-1">Keep your account secure with a strong password.</p>
              </div>
              <div className="p-8 space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <Input label="Current Password" type="password" placeholder="••••••••" />
                  <div className="hidden md:block"></div>
                  <Input label="New Password" type="password" placeholder="••••••••" />
                  <Input label="Confirm New Password" type="password" placeholder="••••••••" />
                </div>
                <div className="p-4 bg-amber-50 dark:bg-amber-500/10 border border-amber-100 dark:border-amber-500/20 rounded-xl">
                  <p className="text-xs font-bold text-amber-700 dark:text-amber-400 flex items-center gap-2">
                    <Shield size={14} /> Password Requirements
                  </p>
                  <ul className="text-[10px] text-amber-600 dark:text-amber-300 mt-2 space-y-1 font-medium list-disc ml-4">
                    <li>Minimum 8 characters long</li>
                    <li>At least one uppercase letter</li>
                    <li>At least one number or special character</li>
                  </ul>
                </div>
                <div className="flex justify-end pt-4 border-t border-border-subtle">
                  <Button variant="primary">Update Password</Button>
                </div>
              </div>
            </Card>
          )}

          {activeTab === 'notifications' && (
            <Card className="animate-fade-in" padding="p-0 overflow-hidden">
               <div className="px-8 py-6 border-b border-border-subtle bg-surface-1/50">
                <h3 className="text-xl font-bold text-text-main">Notification Preferences</h3>
                <p className="text-sm text-text-muted font-medium mt-1">Choose how you want to be notified about updates.</p>
              </div>
              <div className="p-8 space-y-6">
                <NotificationToggle 
                  title="Email Notifications" 
                  desc="Receive emails about task assignments and mentions." 
                  enabled={true}
                />
                <NotificationToggle 
                  title="Browser Push" 
                  desc="Get desktop notifications while you're active on TaskHub." 
                  enabled={false}
                />
                <NotificationToggle 
                  title="Weekly Digest" 
                  desc="A summary of your team's progress delivered to your inbox." 
                  enabled={true}
                />
              </div>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}

function NotificationToggle({ title, desc, enabled }) {
  const [isOn, setIsOn] = useState(enabled);
  return (
    <div className="flex items-center justify-between py-4 first:pt-0 last:pb-0">
      <div>
        <h4 className="font-bold text-text-main text-sm">{title}</h4>
        <p className="text-xs text-text-muted font-medium">{desc}</p>
      </div>
      <button 
        onClick={() => setIsOn(!isOn)}
        className={`
          w-12 h-6 rounded-full transition-all duration-300 relative
          ${isOn ? 'bg-primary shadow-lg shadow-primary/30' : 'bg-surface-3'}
        `}
      >
        <div className={`
          absolute top-1 w-4 h-4 rounded-full bg-white transition-all duration-300
          ${isOn ? 'left-7' : 'left-1'}
        `}></div>
      </button>
    </div>
  );
}
