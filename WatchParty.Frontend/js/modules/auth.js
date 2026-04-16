import { fetchApi, setToken, setUser, clearAuth, getUser, getToken, isAuthenticated } from '../utils/config.js';
import { success, error } from '../utils/toast.js';

export const auth = {
    async register(username, email, password) {
        try {
            const response = await fetchApi('/auth/register', {
                method: 'POST',
                body: JSON.stringify({ username, email, password })
            });

            setToken(response.token);
            setUser(response.user);
            success('Cuenta creada exitosamente');
            return response;
        } catch (err) {
            error(err.message);
            throw err;
        }
    },

    async login(email, password) {
        try {
            const response = await fetchApi('/auth/login', {
                method: 'POST',
                body: JSON.stringify({ email, password })
            });

            setToken(response.token);
            setUser(response.user);
            success('Sesión iniciada');
            return response;
        } catch (err) {
            error(err.message);
            throw err;
        }
    },

    logout() {
        clearAuth();
        window.location.href = '/';
    },

    async getCurrentUser() {
        try {
            const user = await fetchApi('/auth/me');
            setUser(user);
            return user;
        } catch (err) {
            clearAuth();
            return null;
        }
    },

    getUser,
    getToken,
    isAuthenticated
};
