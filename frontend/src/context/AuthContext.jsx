import { Fragment, useReducer, useEffect, useCallback, useRef } from 'react';
import { authApi } from '@/api/authApi';
import { getAccessToken, getSessionVersion, setAccessToken, onSessionCleared } from '@/api/axiosInstance';
import { resetUserState } from '@/stores/resetUserState';

import AuthContext from '@/context/authState';

const initialState = {
  user: null,
  isLoading: true,
  isAuthenticated: false,
};

function authReducer(state, action) {
  switch (action.type) {
    case 'AUTH_SUCCESS':
      return {
        ...state,
        user: action.payload.user,
        isLoading: false,
        isAuthenticated: true,
      };
    case 'PROFILE_UPDATED':
      return { ...state, user: action.payload };
    case 'LOGOUT':
      return { ...initialState, isLoading: false };
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    default:
      return state;
  }
}

export function AuthProvider({ children }) {
  const [state, dispatch] = useReducer(authReducer, initialState);
  const operation = useRef(0);
  const identity = useRef(null);
  const acceptSession = useCallback((session) => {
    if (identity.current !== session.user.id) resetUserState();
    identity.current = session.user.id;
    setAccessToken(session.token);
    dispatch({ type: 'AUTH_SUCCESS', payload: session });
  }, []);

  useEffect(() => onSessionCleared(() => {
    operation.current++;
    identity.current = null;
    resetUserState();
    dispatch({ type: 'LOGOUT' });
  }), []);

  // Attempt silent refresh on mount (check httpOnly cookie)
  useEffect(() => {
    let active = true;
    const version = operation.current;
    const initAuth = async () => {
      try {
        const { data } = await authApi.refresh();
        if (!active || version !== operation.current) return;
        if (data.success) {
          acceptSession(data.data);
        } else {
          dispatch({ type: 'LOGOUT' });
        }
      } catch {
        if (active && version === operation.current) dispatch({ type: 'LOGOUT' });
      }
    };
    initAuth();
    return () => { active = false; };
  }, [acceptSession]);

  const login = useCallback(async (email, password) => {
    const version = ++operation.current;
    const { data } = await authApi.login({ email, password });
    if (version !== operation.current) return;
    if (!data.success) throw new Error(data.message || 'Login failed');
    acceptSession(data.data);
    return data.data;
  }, [acceptSession]);

  const register = useCallback(async (fullName, email, password) => {
    const version = ++operation.current;
    const { data } = await authApi.register({ fullName, email, password });
    if (version !== operation.current) return;
    if (!data.success) throw new Error(data.message || 'Registration failed');
    acceptSession(data.data);
    return data.data;
  }, [acceptSession]);

  const googleLogin = useCallback(async (credential) => {
    const version = ++operation.current;
    const { data } = await authApi.googleLogin(credential);
    if (version !== operation.current) return;
    if (!data.success) throw new Error(data.message || 'Google Login failed');
    acceptSession(data.data);
    return data.data;
  }, [acceptSession]);

  const logout = useCallback(async () => {
    // Keep credentials only for this request; clear user state immediately.
    const token = getAccessToken();
    setAccessToken(null);
    const version = getSessionVersion();
    const logoutOperation = operation.current;
    try {
      await authApi.logout(token);
    } catch {
      /* ignore logout errors */
    } finally {
      // Invalidate late refreshes without clearing a superseding login.
      if (version === getSessionVersion() && logoutOperation === operation.current)
        setAccessToken(null);
    }
  }, []);

  const refreshProfile = useCallback(async () => {
    const version = operation.current;
    const { data } = await authApi.getMe();
    if (!data.success) throw new Error(data.message || 'Could not refresh profile');
    if (version === operation.current && identity.current === data.data.id)
      dispatch({ type: 'PROFILE_UPDATED', payload: data.data });
    return data.data;
  }, []);

  return (
    <AuthContext.Provider value={{ ...state, login, googleLogin, register, logout, refreshProfile }}>
      <Fragment key={state.user?.id || 'anonymous'}>{children}</Fragment>
    </AuthContext.Provider>
  );
}
