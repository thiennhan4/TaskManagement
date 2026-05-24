import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import * as taskService from '@/services/taskService';
import toast from 'react-hot-toast';
import { CheckCircle, XCircle, Loader2 } from 'lucide-react';
import Button from '@/components/ui/Button';

const AcceptTaskInvite = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const navigate = useNavigate();
  const [status, setStatus] = useState('loading'); // loading, success, error
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      setErrorMsg('No invitation token found in URL.');
      return;
    }

    const accept = async () => {
      try {
        await taskService.acceptTaskInvite(token);
        setStatus('success');
        toast.success('Successfully joined the task!');
      } catch (error) {
        setStatus('error');
        setErrorMsg(error.response?.data?.message || 'Failed to accept invitation. It may have expired or is invalid.');
      }
    };

    accept();
  }, [token]);

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-900 flex items-center justify-center p-4">
      <div className="bg-white dark:bg-slate-800 rounded-2xl shadow-premium border border-border-subtle p-8 max-w-md w-full text-center">
        {status === 'loading' && (
          <div className="flex flex-col items-center">
            <Loader2 className="animate-spin text-primary mb-4" size={48} />
            <h2 className="text-xl font-bold text-text-main">Verifying Task Invitation...</h2>
            <p className="text-text-muted mt-2">Please wait while we process your invitation.</p>
          </div>
        )}

        {status === 'success' && (
          <div className="flex flex-col items-center animate-in zoom-in">
            <CheckCircle className="text-emerald-500 mb-4" size={56} />
            <h2 className="text-2xl font-bold text-text-main">Welcome Aboard!</h2>
            <p className="text-text-muted mt-2 mb-6">You have been assigned to the task.</p>
            <Button onClick={() => navigate('/my-tasks')} className="w-full">
              Go to My Tasks
            </Button>
          </div>
        )}

        {status === 'error' && (
          <div className="flex flex-col items-center animate-in zoom-in">
            <XCircle className="text-rose-500 mb-4" size={56} />
            <h2 className="text-2xl font-bold text-text-main">Invitation Failed</h2>
            <p className="text-rose-500 mt-2 mb-6">{errorMsg}</p>
            <Button onClick={() => navigate('/dashboard')} variant="outline" className="w-full">
              Back to Dashboard
            </Button>
          </div>
        )}
      </div>
    </div>
  );
};

export default AcceptTaskInvite;
