import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { projectMemberApi } from '@/api/projectMemberApi';
import { useAuth } from '@/context/AuthContext';
import toast from 'react-hot-toast';
import { CheckCircle, XCircle, Loader2 } from 'lucide-react';
import Button from '@/components/ui/Button';

const AcceptProjectInvite = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const navigate = useNavigate();
  const { isAuthenticated, isLoading } = useAuth();
  const [status, setStatus] = useState('loading'); // loading, success, error
  const [errorMsg, setErrorMsg] = useState('');

  const projectId = searchParams.get('projectId');

  useEffect(() => {
    if (isLoading) {
      return;
    }

    if (!token) {
      setStatus('error');
      setErrorMsg('No invitation token found in URL.');
      return;
    }

    if (!projectId) {
      setStatus('error');
      setErrorMsg('No project ID found in URL.');
      return;
    }

    if (!isAuthenticated) {
      navigate(`/login?redirect=${encodeURIComponent(window.location.pathname + window.location.search)}`, { replace: true });
      return;
    }

    const accept = async () => {
      try {
        await projectMemberApi.acceptInvite(projectId, token);
        setStatus('success');
        toast.success('Successfully joined the project!');
      } catch (error) {
        setStatus('error');
        const apiMessage = error.response?.data?.message || error.response?.data?.Message;
        setErrorMsg(apiMessage || 'Failed to accept invitation. It may have expired, be invalid, or belong to another email.');
      }
    };

    accept();
  }, [token, projectId, isAuthenticated, isLoading, navigate]);

  return (
    <div className="min-h-screen bg-surface-1 flex items-center justify-center p-4">
      <div className="bg-surface-0 rounded-2xl shadow-premium border border-border-subtle p-8 max-w-md w-full text-center">
        {status === 'loading' && (
          <div className="flex flex-col items-center">
            <Loader2 className="animate-spin text-primary mb-4" size={48} />
            <h2 className="text-xl font-bold text-text-main">Verifying Invitation...</h2>
            <p className="text-text-muted mt-2">Please wait while we process your invitation.</p>
          </div>
        )}

        {status === 'success' && (
          <div className="flex flex-col items-center animate-in zoom-in">
            <CheckCircle className="text-emerald-500 mb-4" size={56} />
            <h2 className="text-2xl font-bold text-text-main">Welcome Aboard!</h2>
            <p className="text-text-muted mt-2 mb-6">You have successfully joined the project.</p>
            <Button onClick={() => navigate('/projects')} className="w-full">
              Go to Projects
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

export default AcceptProjectInvite;
