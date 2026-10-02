import { useNavigate, useParams } from 'react-router-dom';
import TaskModal from '@/components/tasks/TaskModal';

export default function TaskDetailPage() {
  const { taskId } = useParams();
  const navigate = useNavigate();
  return <TaskModal isOpen taskId={taskId} onClose={() => navigate('/my-tasks')} />;
}
