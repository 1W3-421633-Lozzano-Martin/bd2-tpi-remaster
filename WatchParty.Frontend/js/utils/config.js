const API_URL = 'http://localhost:5000/api';
const HUB_URL = 'http://localhost:5000/hubs/watchparty';

export const config = {
    apiUrl: API_URL,
    hubUrl: HUB_URL,
    storageKeys: {
        token: 'watchparty_token',
        user: 'watchparty_user'
    }
};

export function getToken() {
    return localStorage.getItem(config.storageKeys.token);
}

export function setToken(token) {
    localStorage.setItem(config.storageKeys.token, token);
}

export function getUser() {
    const user = localStorage.getItem(config.storageKeys.user);
    return user ? JSON.parse(user) : null;
}

export function setUser(user) {
    localStorage.setItem(config.storageKeys.user, JSON.stringify(user));
}

export function clearAuth() {
    localStorage.removeItem(config.storageKeys.token);
    localStorage.removeItem(config.storageKeys.user);
}

export function isAuthenticated() {
    return !!getToken();
}

export async function fetchApi(endpoint, options = {}) {
    const token = getToken();
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${config.apiUrl}${endpoint}`, {
        ...options,
        headers
    });

    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'Request failed' }));
        throw new Error(error.message || 'Request failed');
    }

    return response.json();
}

export function formatDuration(seconds) {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);

    if (hours > 0) {
        return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }
    return `${minutes}:${secs.toString().padStart(2, '0')}`;
}

export function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('es-ES', {
        day: 'numeric',
        month: 'short',
        year: 'numeric'
    });
}

export function timeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);

    if (seconds < 60) return 'ahora';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h`;
    return formatDate(dateString);
}
