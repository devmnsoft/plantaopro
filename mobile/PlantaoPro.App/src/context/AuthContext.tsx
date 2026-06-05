import React, { createContext, PropsWithChildren, useContext, useMemo, useState } from 'react';
import { LoginRequest, UserProfile } from '../types/auth.types';
import * as authService from '../services/authService';

type AuthContextValue = { user: UserProfile | null; signedIn: boolean; signIn: (payload: LoginRequest) => Promise<string | null>; signOut: () => Promise<void>; };
const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const value = useMemo<AuthContextValue>(() => ({
    user,
    signedIn: Boolean(user),
    async signIn(payload) {
      const login = await authService.login(payload);
      if (!login.success) return login.message;
      const me = await authService.getMe();
      setUser(me.data ?? { nome: payload.email, email: payload.email });
      return null;
    },
    async signOut() { await authService.logout(); setUser(null); },
  }), [user]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() { const ctx = useContext(AuthContext); if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider'); return ctx; }
