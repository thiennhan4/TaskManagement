import React, { useRef, useCallback, useState, useEffect } from 'react';
import { Upload, File, Trash2, Download, Loader2, Paperclip } from 'lucide-react';
import { attachmentApi } from '@/api/attachmentApi';
import { toast } from 'react-hot-toast';

export const TaskAttachments = ({ taskId }) => {
  const [attachments, setAttachments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const fileInputRef = useRef(null);



  const fetchAttachments = useCallback(() => {
    return attachmentApi.list(taskId).then(result => {
      if (result.success) {
        setAttachments(result.data);
        setError('');
      }
    }).catch(error => {
      setError(error.response?.data?.message || 'Failed to load attachments');
    }).finally(() => setLoading(false));
  }, [taskId]);

  useEffect(() => {
    fetchAttachments();
  }, [fetchAttachments]);

  const handleUpload = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    if (file.size > 10 * 1024 * 1024) {
      toast.error('File size must be less than 10MB');
      return;
    }

    setUploading(true);
    try {
      const result = await attachmentApi.upload(taskId, file);
      if (result.success) {
        toast.success('File uploaded successfully');
        fetchAttachments();
      }
    } catch {
      toast.error('Upload failed');
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleDelete = async (attachmentId) => {
    try {
      const result = await attachmentApi.remove(taskId, attachmentId);
      if (result.success) {
        toast.success('File deleted');
        setAttachments(previous => previous.filter(a => a.id !== attachmentId));
      }
    } catch {
      toast.error('Failed to delete file');
    }
  };

  const handleDownload = async (file) => {
    try {
      const blob = await attachmentApi.download(taskId, file.id);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = file.fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch {
      toast.error('Download failed. Check that you still have access to this task.');
    }
  };

  return (
    <div className="animate-in fade-in slide-in-from-bottom-2 duration-300">
      {/* Upload Area */}
      <div 
        onClick={() => fileInputRef.current?.click()}
        className={`
          mb-8 border-2 border-dashed rounded-2xl p-10 flex flex-col items-center justify-center gap-3 cursor-pointer transition-all
          ${uploading ? 'bg-surface-2 border-primary/50 cursor-wait' : 'border-border-subtle hover:border-primary/50 hover:bg-hover-bg'}
        `}
      >
        <input 
          type="file" 
          hidden 
          ref={fileInputRef} 
          onChange={handleUpload} 
          disabled={uploading}
        />
        {uploading ? (
          <Loader2 className="w-10 h-10 text-primary animate-spin" />
        ) : (
          <div className="p-4 bg-primary/10 rounded-full text-primary">
            <Upload className="w-8 h-8" />
          </div>
        )}
        <div className="text-center">
          <p className="font-bold text-text-main">
            {uploading ? 'Uploading your file...' : 'Click or drag file to upload'}
          </p>
          <p className="text-xs text-text-muted mt-1 font-medium">PDF, DOC, PNG, JPG (Max 10MB)</p>
        </div>
      </div>

      {/* Attachment List */}
      <div className="space-y-4">
        <h4 className="text-xs font-bold text-text-subtle uppercase tracking-wider mb-4 flex items-center gap-2">
          <Paperclip className="w-3.5 h-3.5" />
          Attachments ({attachments.length})
        </h4>

        {error ? <p role="alert" className="text-text-main">{error}</p> : loading && attachments.length === 0 ? (
          <div className="flex justify-center py-10">
            <Loader2 className="w-6 h-6 text-primary animate-spin" />
          </div>
        ) : attachments.length > 0 ? (
          <div className="grid grid-cols-1 gap-3">
            {attachments.map((file) => (
              <div key={file.id} className="flex items-center gap-4 p-4 bg-surface-0 border border-border-subtle rounded-xl hover:border-primary/30 hover:shadow-sm transition-all group">
                <div className="p-3 bg-surface-2 rounded-xl text-text-subtle group-hover:bg-primary/10 group-hover:text-primary transition-colors">
                  <File className="w-6 h-6" />
                </div>
                
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-bold text-text-main truncate">{file.fileName}</p>
                  <p className="text-[10px] font-medium text-text-subtle uppercase mt-0.5 tracking-wider">
                    {file.fileSize} • By {file.uploadedByUserName}
                  </p>
                </div>

                <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <button
                    onClick={() => handleDownload(file)}
                    className="p-2 text-text-subtle hover:text-primary hover:bg-primary/10 rounded-lg transition-all"
                    title="Download"
                  >
                    <Download className="w-4 h-4" />
                  </button>
                  {file.canDelete && <button
                    onClick={() => handleDelete(file.id)}
                    className="p-2 text-text-subtle hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-950/30 rounded-lg transition-all"
                    title="Delete"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="py-12 text-center bg-surface-2/50 rounded-2xl border border-border-subtle">
            <Paperclip className="w-10 h-10 mx-auto mb-3 text-text-subtle opacity-40" />
            <p className="text-sm font-medium text-text-subtle">No attachments yet</p>
          </div>
        )}
      </div>
    </div>
  );
};
