import { fetchApi } from '../utils/config.js';
import { success, error } from '../utils/toast.js';

export const movies = {
    async search(query = null, genre = null, page = 1, limit = 20) {
        const params = new URLSearchParams();
        if (query) params.append('query', query);
        if (genre) params.append('genre', genre);
        params.append('page', page);
        params.append('limit', limit);
        return await fetchApi(`/movies?${params}`);
    },

    async getPopular(limit = 20) {
        return await fetchApi(`/movies/popular?limit=${limit}`);
    },

    async getById(id) {
        return await fetchApi(`/movies/${id}`);
    },

    async add(data) {
        try {
            const movie = await fetchApi('/movies', {
                method: 'POST',
                body: JSON.stringify(data)
            });
            success('Película añadida exitosamente');
            return movie;
        } catch (err) {
            error('Error al añadir la película');
            throw err;
        }
    },

    async delete(id) {
        try {
            await fetchApi(`/movies/${id}`, {
                method: 'DELETE'
            });
            success('Película eliminada');
        } catch (err) {
            error(err.message);
            throw err;
        }
    }
};
