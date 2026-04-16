import { fetchApi } from '../utils/config.js';
import { success, error } from '../utils/toast.js';

export const rooms = {
    async getActive() {
        return await fetchApi('/rooms');
    },

    async getByCode(code) {
        return await fetchApi(`/rooms/${code.toUpperCase()}`);
    },

    async getState(code) {
        return await fetchApi(`/rooms/${code.toUpperCase()}/state`);
    },

    async create(name, videoUrl = null, isPrivate = false, password = null, maxViewers = 50) {
        try {
            const room = await fetchApi('/rooms', {
                method: 'POST',
                body: JSON.stringify({
                    name,
                    videoUrl,
                    isPrivate,
                    password,
                    maxViewers
                })
            });
            success('Sala creada exitosamente');
            return room;
        } catch (err) {
            error('Error al crear la sala');
            throw err;
        }
    },

    async join(code, password = null) {
        return await fetchApi(`/rooms/${code.toUpperCase()}/join`, {
            method: 'POST',
            body: JSON.stringify({ password })
        });
    },

    async update(code, data) {
        try {
            await fetchApi(`/rooms/${code.toUpperCase()}`, {
                method: 'PUT',
                body: JSON.stringify(data)
            });
            success('Sala actualizada');
        } catch (err) {
            error(err.message);
            throw err;
        }
    },

    async delete(code) {
        try {
            await fetchApi(`/rooms/${code.toUpperCase()}`, {
                method: 'DELETE'
            });
            success('Sala eliminada');
        } catch (err) {
            error(err.message);
            throw err;
        }
    },

    async getUserRooms(userId) {
        return await fetchApi(`/rooms/user/${userId}`);
    }
};
