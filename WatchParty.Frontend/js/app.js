import { auth } from './modules/auth.js';
import { rooms } from './modules/rooms.js';
import { movies } from './modules/movies.js';
import { signalR } from './modules/signalr.js';
import { success, error } from './utils/toast.js';

const app = document.getElementById('app');
const navAuth = document.getElementById('navAuth');
const navUser = document.getElementById('navUser');
const userName = document.getElementById('userName');
const userMenu = document.getElementById('userMenu');
const userDropdown = document.getElementById('userDropdown');
const mobileMenuBtn = document.getElementById('mobileMenuBtn');
const navLinks = document.getElementById('navLinks');

const routes = {
    '/': renderHome,
    '/login': renderLogin,
    '/register': renderRegister,
    '/create-room': renderCreateRoom,
    '/join-room': renderJoinRoom,
    '/room': renderRoom,
    '/movies': renderMovies,
    '/my-rooms': renderMyRooms,
    '/profile': renderProfile
};

function router() {
    const path = window.location.pathname;
    const query = new URLSearchParams(window.location.search);
    
    if (path.startsWith('/room/')) {
        const code = path.split('/room/')[1];
        window.location.href = `/room?code=${code}`;
        return;
    }

    updateNavigation();
    
    const handler = routes[path];
    if (handler) {
        handler(query);
    } else {
        render404();
    }
}

function navigate(path) {
    window.history.pushState(null, null, path);
    router();
}

function updateNavigation() {
    const isAuth = auth.isAuthenticated();
    
    if (isAuth) {
        const user = auth.getUser();
        navAuth.classList.add('hidden');
        navUser.classList.remove('hidden');
        userName.textContent = user.username;
    } else {
        navAuth.classList.remove('hidden');
        navUser.classList.add('hidden');
    }
}

async function renderHome() {
    const template = await fetch('./home.html').then(r => r.text());
    app.innerHTML = template;
    
    const roomsGrid = document.getElementById('roomsGrid');
    try {
        const activeRooms = await rooms.getActive();
        if (activeRooms.length === 0) {
            roomsGrid.innerHTML = '<p class="empty-state">No hay salas activas en este momento.</p>';
        } else {
            roomsGrid.innerHTML = activeRooms.map(room => `
                <div class="room-card" data-code="${room.code}">
                    <div class="room-card-header">
                        <h3>${room.name}</h3>
                        <span class="room-code">${room.code}</span>
                    </div>
                    <div class="room-card-info">
                        <span><i class="fas fa-eye"></i> ${room.viewerCount}</span>
                        ${room.isPrivate ? '<span><i class="fas fa-lock"></i> Privada</span>' : ''}
                    </div>
                    <div class="room-card-creator">
                        <i class="fas fa-user"></i> ${room.creatorUsername}
                    </div>
                </div>
            `).join('');
            
            roomsGrid.querySelectorAll('.room-card').forEach(card => {
                card.addEventListener('click', () => {
                    const code = card.dataset.code;
                    navigate(`/room?code=${code}`);
                });
            });
        }
    } catch (err) {
        roomsGrid.innerHTML = '<p class="error">Error al cargar las salas</p>';
    }
}

async function renderLogin() {
    if (auth.isAuthenticated()) {
        navigate('/');
        return;
    }
    
    const template = await fetch('./login.html').then(r => r.text());
    app.innerHTML = template;
    
    const form = document.getElementById('loginForm');
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const email = form.email.value;
        const password = form.password.value;
        
        try {
            await auth.login(email, password);
            navigate('/');
        } catch (err) {}
    });
}

async function renderRegister() {
    if (auth.isAuthenticated()) {
        navigate('/');
        return;
    }
    
    const template = await fetch('./register.html').then(r => r.text());
    app.innerHTML = template;
    
    const form = document.getElementById('registerForm');
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const username = form.username.value;
        const email = form.email.value;
        const password = form.password.value;
        const confirmPassword = form.confirmPassword.value;
        
        if (password !== confirmPassword) {
            error('Las contraseñas no coinciden');
            return;
        }
        
        try {
            await auth.register(username, email, password);
            navigate('/');
        } catch (err) {}
    });
}

async function renderCreateRoom() {
    if (!auth.isAuthenticated()) {
        error('Debes iniciar sesión para crear una sala');
        navigate('/login');
        return;
    }
    
    const template = await fetch('./create-room.html').then(r => r.text());
    app.innerHTML = template;
    
    const isPrivateCheckbox = document.getElementById('isPrivate');
    const privateOptions = document.getElementById('privateOptions');
    
    isPrivateCheckbox.addEventListener('change', () => {
        privateOptions.classList.toggle('hidden', !isPrivateCheckbox.checked);
    });
    
    const form = document.getElementById('createRoomForm');
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const name = form.roomName.value;
        const videoUrl = form.videoUrl.value || null;
        const isPrivate = form.isPrivate.checked;
        const password = isPrivate ? form.roomPassword.value : null;
        const maxViewers = parseInt(form.maxViewers.value);
        
        try {
            const room = await rooms.create(name, videoUrl, isPrivate, password, maxViewers);
            navigate(`/room?code=${room.code}`);
        } catch (err) {}
    });
}

async function renderJoinRoom() {
    const template = await fetch('./join-room.html').then(r => r.text());
    app.innerHTML = template;
    
    const joinOptions = document.querySelectorAll('.join-option');
    const codeForm = document.getElementById('joinByCodeForm');
    const linkForm = document.getElementById('joinByLinkForm');
    
    joinOptions.forEach(option => {
        option.addEventListener('click', () => {
            joinOptions.forEach(o => o.classList.remove('active'));
            option.classList.add('active');
            
            const tab = option.dataset.tab;
            codeForm.classList.toggle('hidden', tab !== 'code');
            linkForm.classList.toggle('hidden', tab !== 'link');
        });
    });
    
    codeForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const code = codeForm.roomCode.value.toUpperCase();
        navigate(`/room?code=${code}`);
    });
    
    linkForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const link = linkForm.roomLink.value;
        const code = link.split('/room/')[1]?.split('?')[0]?.toUpperCase();
        if (code) {
            navigate(`/room?code=${code}`);
        } else {
            error('Enlace inválido');
        }
    });
}

async function renderRoom(query) {
    const code = query.get('code');
    if (!code) {
        navigate('/');
        return;
    }

    const template = await fetch('./room.html').then(r => r.text());
    app.innerHTML = template;
    
    const roomNameEl = document.getElementById('roomName');
    const roomCodeEl = document.getElementById('roomCode');
    const viewerCountEl = document.getElementById('viewerCount');
    const viewersListEl = document.getElementById('viewersList');
    const chatMessagesEl = document.getElementById('chatMessages');
    const chatForm = document.getElementById('chatForm');
    const chatInput = document.getElementById('chatInput');
    const videoPlayer = document.getElementById('videoPlayer');
    const playPauseBtn = document.getElementById('playPauseBtn');
    const progressBar = document.getElementById('progressBar');
    const progressFilled = document.getElementById('progressFilled');
    const timeDisplay = document.getElementById('timeDisplay');
    const muteBtn = document.getElementById('muteBtn');
    const volumeSlider = document.getElementById('volumeSlider');
    const shareBtn = document.getElementById('shareBtn');
    const shareModal = document.getElementById('shareModal');
    const closeShareModal = document.getElementById('closeShareModal');
    const copyLinkBtn = document.getElementById('copyLinkBtn');
    const shareLink = document.getElementById('shareLink');
    const leaveBtn = document.getElementById('leaveBtn');
    const videoSelector = document.getElementById('videoSelector');
    const movieSearch = document.getElementById('movieSearch');
    const moviesList = document.getElementById('moviesList');
    const startVoteBtn = document.getElementById('startVoteBtn');
    const voteResults = document.getElementById('voteResults');
    const endVoteBtn = document.getElementById('endVoteBtn');
    
    roomCodeEl.textContent = code;
    shareLink.value = `${window.location.origin}/room/${code}`;
    
    try {
        const room = await rooms.getByCode(code);
        roomNameEl.textContent = room.name;
        
        if (room.videoUrl) {
            videoPlayer.src = room.videoUrl;
        }
    } catch (err) {
        error('Sala no encontrada');
        navigate('/');
        return;
    }
    
    try {
        await signalR.connect(code);
        
        signalR.on('roomJoined', (data) => {
            viewerCountEl.textContent = data.state.viewerCount;
            roomNameEl.textContent = data.state.name;
            
            if (data.state.videoUrl) {
                videoPlayer.src = data.state.videoUrl;
            }
            
            viewersListEl.innerHTML = data.viewers.map(v => `
                <li class="viewer-item">
                    <div class="viewer-avatar">${v.username[0].toUpperCase()}</div>
                    <span class="viewer-name">${v.username}</span>
                    ${v.isCreator ? '<span class="viewer-badge">Host</span>' : ''}
                </li>
            `).join('');
            
            chatMessagesEl.innerHTML = data.messages.map(m => `
                <div class="chat-message">
                    <div class="chat-message-header">
                        <span class="chat-message-author">${m.username}</span>
                        <span class="chat-message-time">${new Date(m.createdAt).toLocaleTimeString()}</span>
                    </div>
                    <div class="chat-message-content">${m.content}</div>
                </div>
            `).join('');
            chatMessagesEl.scrollTop = chatMessagesEl.scrollHeight;
        });
        
        signalR.on('viewerJoined', (data) => {
            viewerCountEl.textContent = data.viewerCount;
        });
        
        signalR.on('viewerLeft', (data) => {
            viewerCountEl.textContent = data.viewerCount;
        });
        
        signalR.on('newMessage', (message) => {
            chatMessagesEl.innerHTML += `
                <div class="chat-message">
                    <div class="chat-message-header">
                        <span class="chat-message-author">${message.username}</span>
                        <span class="chat-message-time">${new Date(message.createdAt).toLocaleTimeString()}</span>
                    </div>
                    <div class="chat-message-content">${message.content}</div>
                </div>
            `;
            chatMessagesEl.scrollTop = chatMessagesEl.scrollHeight;
        });
        
        signalR.on('videoSync', (data) => {
            if (data.videoUrl && videoPlayer.src !== data.videoUrl) {
                videoPlayer.src = data.videoUrl;
            }
            videoPlayer.currentTime = data.position;
            if (data.isPlaying) {
                videoPlayer.play();
            } else {
                videoPlayer.pause();
            }
        });
        
        signalR.on('movieChanged', (data) => {
            if (data.MovieId) {
                movies.getById(data.MovieId).then(movie => {
                    videoPlayer.src = movie.videoUrl;
                    videoPlayer.currentTime = 0;
                    videoPlayer.play();
                });
            }
        });
        
        signalR.on('voteStarted', (data) => {
            startVoteBtn.classList.add('hidden');
            voteResults.classList.remove('hidden');
        });
        
        signalR.on('voteEnded', (data) => {
            startVoteBtn.classList.remove('hidden');
            voteResults.classList.add('hidden');
        });
        
    } catch (err) {
        error('Error al conectar con la sala');
    }
    
    chatForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (chatInput.value.trim()) {
            await signalR.sendMessage(chatInput.value.trim());
            chatInput.value = '';
        }
    });
    
    playPauseBtn.addEventListener('click', () => {
        if (videoPlayer.paused) {
            videoPlayer.play();
        } else {
            videoPlayer.pause();
        }
    });
    
    videoPlayer.addEventListener('play', () => {
        playPauseBtn.innerHTML = '<i class="fas fa-pause"></i>';
        const user = auth.getUser();
        if (user) {
            signalR.syncVideo(videoPlayer.src, videoPlayer.currentTime, true);
        }
    });
    
    videoPlayer.addEventListener('pause', () => {
        playPauseBtn.innerHTML = '<i class="fas fa-play"></i>';
        const user = auth.getUser();
        if (user) {
            signalR.syncVideo(videoPlayer.src, videoPlayer.currentTime, false);
        }
    });
    
    videoPlayer.addEventListener('timeupdate', () => {
        const progress = (videoPlayer.currentTime / videoPlayer.duration) * 100;
        progressFilled.style.width = `${progress}%`;
        const current = formatTime(videoPlayer.currentTime);
        const total = formatTime(videoPlayer.duration);
        timeDisplay.textContent = `${current} / ${total}`;
    });
    
    progressBar.addEventListener('click', (e) => {
        const rect = progressBar.getBoundingClientRect();
        const percent = (e.clientX - rect.left) / rect.width;
        videoPlayer.currentTime = percent * videoPlayer.duration;
    });
    
    muteBtn.addEventListener('click', () => {
        videoPlayer.muted = !videoPlayer.muted;
        muteBtn.innerHTML = `<i class="fas fa-volume-${videoPlayer.muted ? 'mute' : 'up'}"></i>`;
    });
    
    volumeSlider.addEventListener('input', (e) => {
        videoPlayer.volume = e.target.value;
    });
    
    shareBtn.addEventListener('click', () => {
        shareModal.classList.remove('hidden');
    });
    
    closeShareModal.addEventListener('click', () => {
        shareModal.classList.add('hidden');
    });
    
    shareModal.addEventListener('click', (e) => {
        if (e.target === shareModal) {
            shareModal.classList.add('hidden');
        }
    });
    
    copyLinkBtn.addEventListener('click', () => {
        navigator.clipboard.writeText(shareLink.value);
        success('Enlace copiado');
    });
    
    leaveBtn.addEventListener('click', async () => {
        await signalR.disconnect();
        navigate('/');
    });
    
    movieSearch.addEventListener('input', async (e) => {
        const query = e.target.value;
        if (query.length >= 2) {
            const results = await movies.search(query);
            renderMoviesList(results);
        } else {
            const popular = await movies.getPopular();
            renderMoviesList(popular);
        }
    });
    
    async function renderMoviesList(movieList) {
        moviesList.innerHTML = movieList.map(m => `
            <div class="movie-item" data-id="${m.id}">
                ${m.thumbnail ? `<img src="${m.thumbnail}" alt="${m.title}">` : '<div class="no-image"></div>'}
                <div class="movie-item-info">
                    <h4>${m.title}</h4>
                    <span>${m.duration || 0} min</span>
                </div>
            </div>
        `).join('');
        
        moviesList.querySelectorAll('.movie-item').forEach(item => {
            item.addEventListener('click', async () => {
                const movie = await movies.getById(item.dataset.id);
                await signalR.changeMovie(movie.id, movie.title, movie.thumbnail);
            });
        });
    }
    
    const popular = await movies.getPopular();
    renderMoviesList(popular);
    
    startVoteBtn.addEventListener('click', async () => {
        const selectedMovie = moviesList.querySelector('.movie-item');
        if (selectedMovie) {
            const movie = await movies.getById(selectedMovie.dataset.id);
            await signalR.startVote(movie.id, movie.title);
        }
    });
    
    endVoteBtn.addEventListener('click', async () => {
        await signalR.endVote();
    });
}

function formatTime(seconds) {
    if (isNaN(seconds)) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
}

async function renderMovies() {
    const template = await fetch('./movies.html').then(r => r.text());
    app.innerHTML = template;
    
    const moviesGrid = document.getElementById('moviesGrid');
    const searchInput = document.getElementById('searchInput');
    const addMovieBtn = document.getElementById('addMovieBtn');
    const addMovieModal = document.getElementById('addMovieModal');
    const closeAddMovieModal = document.getElementById('closeAddMovieModal');
    const cancelAddMovie = document.getElementById('cancelAddMovie');
    const addMovieForm = document.getElementById('addMovieForm');
    
    async function loadMovies(query = '') {
        try {
            const list = query ? await movies.search(query) : await movies.getPopular();
            if (list.length === 0) {
                moviesGrid.innerHTML = '<p class="empty-state">No se encontraron películas</p>';
            } else {
                moviesGrid.innerHTML = list.map(m => `
                    <div class="movie-card">
                        ${m.thumbnail ? `<img src="${m.thumbnail}" alt="${m.title}">` : '<div class="no-image" style="height:280px;background:var(--dark-3);display:flex;align-items:center;justify-content:center;"><i class="fas fa-film" style="font-size:48px;color:var(--gray);"></i></div>'}
                        <div class="movie-card-body">
                            <h3>${m.title}</h3>
                            <div class="movie-card-info">
                                ${m.genre ? `<span>${m.genre}</span>` : ''}
                                ${m.year ? `<span>${m.year}</span>` : ''}
                            </div>
                            <div class="movie-card-footer">
                                <span class="movie-card-views"><i class="fas fa-eye"></i> ${m.viewCount}</span>
                            </div>
                        </div>
                    </div>
                `).join('');
            }
        } catch (err) {
            moviesGrid.innerHTML = '<p class="error">Error al cargar las películas</p>';
        }
    }
    
    await loadMovies();
    
    searchInput.addEventListener('input', debounce(async (e) => {
        await loadMovies(e.target.value);
    }, 500));
    
    addMovieBtn.addEventListener('click', () => {
        if (!auth.isAuthenticated()) {
            error('Debes iniciar sesión para añadir películas');
            navigate('/login');
            return;
        }
        addMovieModal.classList.remove('hidden');
    });
    
    closeAddMovieModal.addEventListener('click', () => {
        addMovieModal.classList.add('hidden');
    });
    
    cancelAddMovie.addEventListener('click', () => {
        addMovieModal.classList.add('hidden');
    });
    
    addMovieForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const data = {
            title: addMovieForm.movieTitle.value,
            description: addMovieForm.movieDescription.value,
            videoUrl: addMovieForm.movieUrl.value,
            thumbnail: addMovieForm.movieThumbnail.value || null,
            duration: parseInt(addMovieForm.movieDuration.value) || 0,
            year: parseInt(addMovieForm.movieYear.value) || null,
            genre: addMovieForm.movieGenre.value || null
        };
        
        try {
            await movies.add(data);
            addMovieModal.classList.add('hidden');
            addMovieForm.reset();
            await loadMovies();
        } catch (err) {}
    });
}

async function renderMyRooms() {
    if (!auth.isAuthenticated()) {
        error('Debes iniciar sesión');
        navigate('/login');
        return;
    }
    
    const template = await fetch('./my-rooms.html').then(r => r.text());
    app.innerHTML = template;
    
    const myRoomsList = document.getElementById('myRoomsList');
    const emptyRooms = document.getElementById('emptyRooms');
    
    try {
        const user = auth.getUser();
        const userRooms = await rooms.getUserRooms(user.id);
        
        if (userRooms.length === 0) {
            myRoomsList.classList.add('hidden');
            emptyRooms.classList.remove('hidden');
        } else {
            myRoomsList.innerHTML = userRooms.map(room => `
                <div class="my-room-item">
                    <div class="my-room-info">
                        <h3>${room.name}</h3>
                        <div class="my-room-meta">
                            <span><i class="fas fa-key"></i> ${room.code}</span>
                            <span><i class="fas fa-eye"></i> ${room.viewerCount} espectadores</span>
                            <span><i class="fas fa-calendar"></i> ${new Date(room.createdAt).toLocaleDateString()}</span>
                        </div>
                    </div>
                    <div class="my-room-actions">
                        <button class="btn btn-primary btn-sm" onclick="location.href='/room?code=${room.code}'">
                            <i class="fas fa-play"></i> Abrir
                        </button>
                    </div>
                </div>
            `).join('');
        }
    } catch (err) {
        myRoomsList.innerHTML = '<p class="error">Error al cargar las salas</p>';
    }
}

async function renderProfile() {
    if (!auth.isAuthenticated()) {
        navigate('/login');
        return;
    }
    
    app.innerHTML = `
        <div class="auth-container">
            <div class="auth-card">
                <h1><i class="fas fa-user"></i> Perfil</h1>
                <p>Cargando...</p>
            </div>
        </div>
    `;
    
    const user = auth.getUser();
    app.innerHTML = `
        <div class="auth-container">
            <div class="auth-card">
                <div class="auth-header">
                    <div class="viewer-avatar" style="width:80px;height:80px;font-size:32px;">
                        ${user.username[0].toUpperCase()}
                    </div>
                    <h1>${user.username}</h1>
                    <p>${user.email}</p>
                </div>
                <div style="margin-top:24px;">
                    <a href="/my-rooms" class="btn btn-primary btn-block" data-link>
                        <i class="fas fa-door-open"></i> Mis Salas
                    </a>
                </div>
            </div>
        </div>
    `;
}

function render404() {
    app.innerHTML = `
        <div class="auth-container">
            <div class="auth-card" style="text-align:center;">
                <i class="fas fa-question-circle" style="font-size:64px;color:var(--gray);margin-bottom:24px;"></i>
                <h1>404</h1>
                <p>Página no encontrada</p>
                <a href="/" class="btn btn-primary" data-link>Volver al inicio</a>
            </div>
        </div>
    `;
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

document.addEventListener('click', (e) => {
    if (e.target.matches('[data-link]')) {
        e.preventDefault();
        navigate(e.target.getAttribute('href'));
    }
});

document.addEventListener('submit', (e) => {
    if (e.target.matches('form')) {
        e.preventDefault();
    }
});

window.addEventListener('popstate', router);

document.addEventListener('DOMContentLoaded', () => {
    if (typeof signalR === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.0/signalr.min.js';
        script.onload = router;
        document.head.appendChild(script);
    } else {
        router();
    }
});

userMenu?.addEventListener('click', () => {
    userDropdown.classList.toggle('hidden');
});

document.addEventListener('click', (e) => {
    if (!userMenu?.contains(e.target)) {
        userDropdown?.classList.add('hidden');
    }
});

document.getElementById('logoutBtn')?.addEventListener('click', () => {
    auth.logout();
});

mobileMenuBtn?.addEventListener('click', () => {
    navLinks.classList.toggle('hidden');
});

export { navigate };
