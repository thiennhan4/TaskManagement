# 🎨 TaskHub Frontend Plan

## Stack: React + Vite + Vanilla CSS

---

# 1. 🎯 Mục tiêu

* SPA hiện đại, responsive
* Tích hợp hoàn chỉnh với Backend API
* UX mượt mà, dark mode support
* Quản lý state & auth bằng Context API

---

# 2. 🛠 Tech Stack

| Công nghệ         | Mục đích                       |
| ------------------ | ------------------------------ |
| React 18           | UI framework                   |
| Vite               | Build tool & dev server         |
| React Router v6    | Client-side routing             |
| Axios              | HTTP client                     |
| Context API        | Global state (auth, theme)      |
| Vanilla CSS        | Styling (custom design system)  |
| react-hook-form    | Form management (planned)       |
| zod                | Schema validation (planned)     |

---

# 3. 📂 Cấu trúc thư mục

```txt
taskhub/
├── public/
├── src/
│   ├── api/                    # Axios instance & API calls
│   │   ├── axiosInstance.js    # Base config + interceptors
│   │   ├── authApi.js          # Auth endpoints
│   │   ├── boardApi.js         # Board endpoints
│   │   ├── taskApi.js          # Task endpoints
│   │   └── settingsApi.js      # Settings endpoints
│   │
│   ├── components/             # Reusable UI components
│   │   ├── Navbar/
│   │   ├── Sidebar/
│   │   ├── TaskCard/
│   │   ├── BoardCard/
│   │   ├── Modal/
│   │   └── common/             # Button, Input, Loading...
│   │
│   ├── context/                # React Context providers
│   │   ├── AuthContext.jsx     # Auth state + JWT management
│   │   └── ThemeContext.jsx    # Dark/Light mode
│   │
│   ├── pages/                  # Page-level components
│   │   ├── LoginPage.jsx
│   │   ├── RegisterPage.jsx
│   │   ├── DashboardPage.jsx
│   │   ├── BoardPage.jsx
│   │   ├── TaskDetailPage.jsx
│   │   ├── SettingsPage.jsx
│   │   └── NotFoundPage.jsx
│   │
│   ├── styles/                 # Global & shared styles
│   │   ├── variables.css       # CSS custom properties
│   │   ├── reset.css           # CSS reset
│   │   └── components/        # Component-specific styles
│   │
│   ├── utils/                  # Helper functions
│   │   ├── dateUtils.js
│   │   ├── formatUtils.js
│   │   └── storageUtils.js
│   │
│   ├── hooks/                  # Custom React hooks (planned)
│   │   ├── useAuth.js
│   │   ├── useBoards.js
│   │   └── useTasks.js
│   │
│   ├── App.jsx                 # Root component + routing
│   ├── main.jsx                # Entry point
│   └── index.css               # Global styles
│
├── index.html
├── vite.config.js
└── package.json
```

---

# 4. 🔐 Authentication Flow

## 4.1 Login Flow

```txt
User submit → authApi.login() → Save tokens → Redirect to Dashboard
```

## 4.2 Token Management

* Access Token: lưu trong memory (Context)
* Refresh Token: lưu trong localStorage hoặc httpOnly cookie
* Auto-refresh khi access token hết hạn

## 4.3 Protected Routes

```jsx
// App.jsx
<Route path="/dashboard" element={
    <PrivateRoute>
        <DashboardPage />
    </PrivateRoute>
} />
```

## 4.4 Axios Interceptors

* **Request**: tự động gắn `Authorization: Bearer <token>`
* **Response**: nếu 401 → thử refresh token → nếu fail → redirect login

---

# 5. 📄 Các trang chính

## 5.1 Auth Pages

| Trang      | Route       | Mô tả                     |
| ---------- | ----------- | -------------------------- |
| Login      | `/login`    | Form đăng nhập             |
| Register   | `/register` | Form đăng ký               |

## 5.2 Main Pages

| Trang      | Route              | Mô tả                           |
| ---------- | ------------------ | -------------------------------- |
| Dashboard  | `/dashboard`       | Overview stats, recent tasks      |
| Board      | `/boards/:id`      | Kanban board với lists & tasks    |
| Settings   | `/settings`        | Profile, password, activity log   |

## 5.3 Error Pages

| Trang      | Route | Mô tả         |
| ---------- | ----- | -------------- |
| 404        | `*`   | Page not found |

---

# 6. 🎨 Design System

## 6.1 Color Palette

```css
:root {
    --primary: #6C5CE7;
    --primary-dark: #5A4BD1;
    --secondary: #00CEC9;
    --bg-dark: #1A1A2E;
    --bg-card: #16213E;
    --text-primary: #EAEAEA;
    --text-secondary: #A0A0B0;
    --success: #00B894;
    --warning: #FDCB6E;
    --danger: #E17055;
}
```

## 6.2 Typography

* Font: Inter / Roboto
* Heading: 600-700 weight
* Body: 400 weight

## 6.3 Components

* Cards với border-radius, subtle shadow
* Glassmorphism cho modals
* Smooth transitions (0.2s ease)
* Hover effects trên interactive elements

---

# 7. 📊 State Management

## AuthContext

```txt
- user: { id, email, fullName, role, avatarUrl }
- token: string
- isAuthenticated: boolean
- login(), logout(), refreshToken()
```

## Board State (Component-level)

```txt
- boards: Board[]
- selectedBoard: Board
- lists: BoardList[]
- tasks: TaskItem[]
```

---

# 8. 🔗 API Integration

## Base URL

```txt
Development: http://localhost:5000/api
```

## Response Format

Mọi API trả về `ApiResponse<T>`:
```json
{
    "success": true,
    "message": "...",
    "data": { ... },
    "errors": null
}
```

---

# 9. 🚀 Development Phases

## Phase 1 ✅

* [x] Project setup (Vite + React)
* [x] Login / Register pages
* [x] Auth context + JWT
* [x] Dashboard page
* [x] Board CRUD
* [x] Task CRUD (Kanban board)
* [x] Settings page (profile, password)
* [x] Navbar + navigation

## Phase 2

* [ ] Drag & Drop tasks between lists
* [ ] Real-time notifications
* [ ] Team management UI
* [ ] Comment system UI
* [ ] Dark/Light mode toggle

## Phase 3

* [ ] Advanced filtering & search
* [ ] Calendar view
* [ ] Performance optimization
* [ ] PWA support

---

# 10. ⚠️ Frontend Rules

* Component PascalCase, file PascalCase (`TaskCard.jsx`)
* Hook camelCase với `use` prefix (`useAuth.js`)
* API files camelCase (`authApi.js`)
* Event handler: `handle` + PascalCase (`handleSubmit`)
* KHÔNG dùng inline styles — dùng CSS classes
* KHÔNG gọi API trực tiếp trong component — dùng api layer
* Luôn handle loading state và error state
* Responsive: Mobile-first approach

---
