$src = 'd:\web\TaskHub\taskhub\src'

# Create directories
$dirs = @(
  'api', 'assets\images', 'assets\icons', 'assets\fonts',
  'components\ui', 'components\layout', 'components\dashboard', 'components\boards',
  'components\tasks', 'components\teams', 'components\common',
  'context', 'hooks',
  'pages\auth', 'pages\dashboard', 'pages\boards', 'pages\tasks', 'pages\teams', 'pages\profile', 'pages\landing',
  'routes', 'styles', 'utils', 'types'
)
foreach ($dir in $dirs) {
  New-Item -ItemType Directory -Force -Path "$src\$dir" | Out-Null
}

# Move and rename existing files
$moves = @{
  'api\axiosInstance.js' = 'api\axios.config.js'
  'components\Navbar.jsx' = 'components\layout\Navbar.jsx'
  'components\ProtectedRoute.jsx' = 'routes\PrivateRoute.jsx'
  'components\Modal.jsx' = 'components\ui\Modal.jsx'
  'components\Tag.jsx' = 'components\ui\Badge.jsx'
  'components\Kanban\BoardList.jsx' = 'components\boards\KanbanColumn.jsx'
  'components\Kanban\TaskCard.jsx' = 'components\tasks\TaskCard.jsx'
  'components\Task\CommentSection.jsx' = 'components\tasks\TaskComments.jsx'
  'components\Task\TaskDetailModal.jsx' = 'components\tasks\TaskModal.jsx'
  'pages\Landing.jsx' = 'pages\landing\LandingPage.jsx'
  'pages\Login.jsx' = 'pages\auth\LoginPage.jsx'
  'pages\Register.jsx' = 'pages\auth\RegisterPage.jsx'
  'pages\Dashboard.jsx' = 'pages\dashboard\DashboardPage.jsx'
  'pages\BoardPage.jsx' = 'pages\boards\BoardDetailPage.jsx'
  'pages\MyTasks.jsx' = 'pages\tasks\MyTasksPage.jsx'
  'pages\TeamsPage.jsx' = 'pages\teams\TeamsPage.jsx'
  'pages\TeamDetailPage.jsx' = 'pages\teams\TeamDetailPage.jsx'
  'index.css' = 'styles\index.css'
}

foreach ($key in $moves.Keys) {
  if (Test-Path "$src\$key") {
    $dest = $moves[$key]
    Move-Item -Force "$src\$key" "$src\$dest"
  }
}

# Touch new empty files
$files = @(
  'components\ui\Button.jsx', 'components\ui\Card.jsx', 'components\ui\Avatar.jsx', 'components\ui\Dropdown.jsx', 'components\ui\Input.jsx', 'components\ui\Textarea.jsx', 'components\ui\Select.jsx', 'components\ui\index.js',
  'components\layout\AppLayout.jsx', 'components\layout\Sidebar.jsx', 'components\layout\Footer.jsx', 'components\layout\AuthLayout.jsx',
  'components\dashboard\DashboardHeader.jsx', 'components\dashboard\StatsCards.jsx', 'components\dashboard\RecentActivity.jsx', 'components\dashboard\BoardsGrid.jsx', 'components\dashboard\BoardCard.jsx', 'components\dashboard\EmptyBoardsState.jsx', 'components\dashboard\CreateBoardButton.jsx',
  'components\boards\BoardView.jsx', 'components\boards\BoardHeader.jsx', 'components\boards\BoardSettings.jsx', 'components\boards\KanbanBoard.jsx', 'components\boards\ListView.jsx', 'components\boards\BoardMembers.jsx',
  'components\tasks\TaskForm.jsx', 'components\tasks\TaskDetails.jsx', 'components\tasks\TaskAttachments.jsx', 'components\tasks\TaskActivity.jsx',
  'components\teams\TeamCard.jsx', 'components\teams\TeamList.jsx', 'components\teams\TeamMembers.jsx', 'components\teams\InviteMemberModal.jsx', 'components\teams\MemberRow.jsx',
  'components\common\LoadingSpinner.jsx', 'components\common\ErrorBoundary.jsx', 'components\common\NotFound.jsx', 'components\common\SearchBar.jsx', 'components\common\DatePicker.jsx', 'components\common\FileUpload.jsx',
  'context\ThemeContext.jsx', 'context\NotificationContext.jsx',
  'hooks\useAuth.js', 'hooks\useBoards.js', 'hooks\useTasks.js', 'hooks\useTeams.js', 'hooks\useDebounce.js', 'hooks\useLocalStorage.js',
  'pages\auth\ForgotPasswordPage.jsx',
  'pages\boards\BoardsListPage.jsx',
  'pages\profile\ProfilePage.jsx',
  'routes\AppRoutes.jsx', 'routes\PublicRoute.jsx',
  'styles\tailwind.css', 'styles\custom.css',
  'utils\format.js', 'utils\validation.js', 'utils\constants.js', 'utils\helpers.js',
  'types\board.types.ts', 'types\task.types.ts', 'types\user.types.ts'
)

foreach ($f in $files) {
  if (!(Test-Path "$src\$f")) {
    New-Item -ItemType File -Force -Path "$src\$f" | Out-Null
  }
}
