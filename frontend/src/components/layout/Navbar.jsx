import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '@/context/AuthContext';

const NAV_HEIGHT = 64;

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav
      style={{
        display: 'flex',
        alignItems: 'stretch',
        height: `${NAV_HEIGHT}px`,
        width: '100%',
        position: 'sticky',
        top: 0,
        zIndex: 50,
        backgroundColor: '#1a1a2e',
        borderBottom: '3px solid #1a1a2e',
      }}
    >
      {/* Logo */}
      <Link
        to="/"
        style={{
          display: 'flex',
          alignItems: 'center',
          padding: '0 24px',
          backgroundColor: '#f7c948',
          borderRight: '3px solid #1a1a2e',
          textDecoration: 'none',
          flexShrink: 0,
        }}
      >
        <span
          style={{
            fontFamily: "'Space Grotesk', sans-serif",
            fontWeight: 900,
            fontSize: '20px',
            color: '#1a1a2e',
            textTransform: 'uppercase',
            letterSpacing: '1px',
            display: 'flex',
            alignItems: 'center',
          }}
        >
          TASK
          <span
            style={{
              backgroundColor: '#1a1a2e',
              color: '#f7c948',
              padding: '2px 8px',
              marginLeft: '4px',
              lineHeight: 1.2,
            }}
          >
            HUB
          </span>
        </span>
      </Link>

      {/* Right side */}
      <div style={{ display: 'flex', alignItems: 'stretch', marginLeft: 'auto' }}>
        {/* Nav links */}
        {!user ? (
          <>
            {[
              { label: 'FEATURES', to: '/#' },
              { label: 'PRICING', to: '/#' },
              { label: 'SIGN IN', to: '/login' },
            ].map((item, idx) => (
              <Link
                key={item.label}
                to={item.to}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  padding: '0 28px',
                  color: 'rgba(255,255,255,0.65)',
                  fontFamily: "'Space Grotesk', sans-serif",
                  fontWeight: 800,
                  fontSize: '11px',
                  textTransform: 'uppercase',
                  letterSpacing: '2px',
                  textDecoration: 'none',
                  borderRight: idx < 2 ? '1px solid rgba(255,255,255,0.1)' : 'none',
                  transition: 'color 0.2s',
                }}
                onMouseEnter={e => e.currentTarget.style.color = '#ffffff'}
                onMouseLeave={e => e.currentTarget.style.color = 'rgba(255,255,255,0.65)'}
              >
                {item.label}
              </Link>
            ))}

            {/* CTA Button */}
            <Link
              to="/register"
              style={{
                display: 'flex',
                alignItems: 'center',
                padding: '0 28px',
                backgroundColor: '#4ecdc4',
                color: '#1a1a2e',
                fontFamily: "'Space Grotesk', sans-serif",
                fontWeight: 900,
                fontSize: '11px',
                textTransform: 'uppercase',
                letterSpacing: '2px',
                textDecoration: 'none',
                borderLeft: '3px solid #1a1a2e',
                gap: '6px',
                whiteSpace: 'nowrap',
                transition: 'filter 0.2s',
              }}
              onMouseEnter={e => e.currentTarget.style.filter = 'brightness(0.92)'}
              onMouseLeave={e => e.currentTarget.style.filter = 'brightness(1)'}
            >
              GET STARTED →
            </Link>
          </>
        ) : (
          <>
            {[
              { label: 'DASHBOARD', to: '/dashboard' },
              { label: 'TEAMS', to: '/teams' },
              { label: 'MY TASKS', to: '/my-tasks' },
            ].map((item, idx) => (
              <Link
                key={item.label}
                to={item.to}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  padding: '0 28px',
                  color: 'rgba(255,255,255,0.65)',
                  fontFamily: "'Space Grotesk', sans-serif",
                  fontWeight: 800,
                  fontSize: '11px',
                  textTransform: 'uppercase',
                  letterSpacing: '2px',
                  textDecoration: 'none',
                  borderRight: '1px solid rgba(255,255,255,0.1)',
                  transition: 'color 0.2s',
                }}
                onMouseEnter={e => e.currentTarget.style.color = '#ffffff'}
                onMouseLeave={e => e.currentTarget.style.color = 'rgba(255,255,255,0.65)'}
              >
                {item.label}
              </Link>
            ))}
            
            <button
              onClick={handleLogout}
              style={{
                display: 'flex',
                alignItems: 'center',
                padding: '0 28px',
                backgroundColor: '#ff6b6b',
                color: '#1a1a2e',
                fontFamily: "'Space Grotesk', sans-serif",
                fontWeight: 900,
                fontSize: '11px',
                textTransform: 'uppercase',
                letterSpacing: '2px',
                border: 'none',
                borderLeft: '3px solid #1a1a2e',
                gap: '6px',
                whiteSpace: 'nowrap',
                cursor: 'pointer',
                transition: 'filter 0.2s',
              }}
              onMouseEnter={e => e.currentTarget.style.filter = 'brightness(0.92)'}
              onMouseLeave={e => e.currentTarget.style.filter = 'brightness(1)'}
            >
              LOGOUT
            </button>
          </>
        )}
      </div>
    </nav>
  );
}

export { NAV_HEIGHT };