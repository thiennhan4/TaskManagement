import TaskDetailPage from '@/pages/tasks/TaskDetailPage';
import { Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider } from '@/context/ThemeContext';
import { AuthProvider } from '@/context/AuthContext';
import { LanguageProvider } from '@/context/LanguageContext';
import { NotificationProvider } from '@/context/NotificationContext';
import AppLayout from '@/components/layout/AppLayout';
import PrivateRoute from '@/routes/PrivateRoute';
import LandingPage from '@/pages/landing/LandingPage';
import LoginPage from '@/pages/auth/LoginPage';
import RegisterPage from '@/pages/auth/RegisterPage';
import DashboardPage from '@/pages/dashboard/DashboardPage';
import BoardDetailPage from '@/pages/boards/BoardDetailPage';
import MyTasksPage from '@/pages/tasks/MyTasksPage';
import TeamsPage from '@/pages/teams/TeamsPage';
import TeamDetailPage from '@/pages/teams/TeamDetailPage';
import ProfilePage from '@/pages/profile/ProfilePage';
import ProjectsPage from '@/pages/projects/ProjectsPage';
import ProjectDetailPage from '@/pages/projects/ProjectDetailPage';
import CalendarPage from '@/pages/calendar/CalendarPage';
import AcceptTaskInvite from '@/pages/tasks/AcceptTaskInvite';
import AcceptProjectInvite from '@/pages/projects/AcceptProjectInvite';
import NotificationsPage from '@/pages/notifications/NotificationsPage';
import { Toaster } from 'react-hot-toast';

function App() {
  return (
    <ThemeProvider>
      <LanguageProvider>
        <AuthProvider>
          <NotificationProvider>
            <Toaster position="top-right" reverseOrder={false} />

            <Routes>
              {/* Public Routes */}
              <Route path="/" element={<LandingPage />} />
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/accept-task-invite" element={<AcceptTaskInvite />} />
              <Route path="/accept-project-invite" element={<AcceptProjectInvite />} />

              {/* Private Dashboard Routes */}
              <Route element={<PrivateRoute><AppLayout /></PrivateRoute>}>
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/tasks/:taskId" element={<TaskDetailPage />} />
                <Route path="/my-tasks" element={<MyTasksPage />} />
                <Route path="/boards/:id" element={<BoardDetailPage />} />
                <Route path="/teams" element={<TeamsPage />} />
                <Route path="/teams/:id" element={<TeamDetailPage />} />
                <Route path="/settings" element={<ProfilePage />} />
                
                {/* Projects */}
                <Route path="/projects" element={<ProjectsPage />} />
                <Route path="/projects/:id" element={<ProjectDetailPage />} />
                <Route path="/projects/archived" element={<ProjectsPage isArchivedView />} />
                
                {/* Calendar */}
                <Route path="/calendar" element={<CalendarPage />} />
                
                {/* Notifications */}
                <Route path="/notifications" element={<NotificationsPage />} />
                
                {/* DASH-004: Redirect /analytics → /dashboard (preserve bookmarks) */}
                <Route path="/analytics" element={<Navigate to="/dashboard" replace />} />
                
                <Route path="/boards" element={<DashboardPage />} />
              </Route>

              {/* Catch-all redirect */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </NotificationProvider>
        </AuthProvider>
      </LanguageProvider>
    </ThemeProvider>
  );
}

export default App;
