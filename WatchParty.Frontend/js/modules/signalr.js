import { getToken, getUser } from '../utils/config.js';

class SignalRService {
    constructor() {
        this.connection = null;
        this.roomCode = null;
        this.callbacks = {};
    }

    async connect(roomCode) {
        this.roomCode = roomCode;
        const token = getToken();
        const user = getUser();

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${window.location.protocol}//${window.location.hostname}:5000/hubs/watchparty`, {
                accessTokenFactory: () => token,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        this.setupEventHandlers();

        try {
            await this.connection.start();
            await this.connection.invoke('JoinRoom', roomCode, null);
        } catch (err) {
            console.error('SignalR connection error:', err);
            throw err;
        }
    }

    setupEventHandlers() {
        this.connection.on('RoomJoined', (data) => {
            this.emit('roomJoined', data);
        });

        this.connection.on('ViewerJoined', (data) => {
            this.emit('viewerJoined', data);
        });

        this.connection.on('ViewerLeft', (data) => {
            this.emit('viewerLeft', data);
        });

        this.connection.on('ViewerDisconnected', (data) => {
            this.emit('viewerDisconnected', data);
        });

        this.connection.on('NewMessage', (message) => {
            this.emit('newMessage', message);
        });

        this.connection.on('VideoSync', (data) => {
            this.emit('videoSync', data);
        });

        this.connection.on('MovieChanged', (data) => {
            this.emit('movieChanged', data);
        });

        this.connection.on('VoteStarted', (data) => {
            this.emit('voteStarted', data);
        });

        this.connection.on('VoteUpdated', (data) => {
            this.emit('voteUpdated', data);
        });

        this.connection.on('VoteEnded', (data) => {
            this.emit('voteEnded', data);
        });

        this.connection.on('Error', (message) => {
            this.emit('error', message);
        });
    }

    async disconnect() {
        if (this.connection && this.roomCode) {
            try {
                await this.connection.invoke('LeaveRoom', this.roomCode);
            } catch (err) {
                console.error('Leave room error:', err);
            }
            await this.connection.stop();
            this.connection = null;
            this.roomCode = null;
        }
    }

    async sendMessage(content, type = 'text') {
        if (this.connection) {
            await this.connection.invoke('SendMessage', content, type);
        }
    }

    async syncVideo(videoUrl, position, isPlaying) {
        if (this.connection) {
            await this.connection.invoke('SyncVideo', videoUrl, position, isPlaying);
        }
    }

    async changeMovie(movieId, movieTitle, thumbnail) {
        if (this.connection) {
            await this.connection.invoke('ChangeMovie', movieId, movieTitle, thumbnail);
        }
    }

    async startVote(movieId, movieTitle, durationSeconds = 60) {
        if (this.connection) {
            await this.connection.invoke('StartVote', movieId, movieTitle, durationSeconds);
        }
    }

    async castVote(movieId) {
        if (this.connection) {
            await this.connection.invoke('CastVote', movieId);
        }
    }

    async endVote() {
        if (this.connection) {
            await this.connection.invoke('EndVote');
        }
    }

    on(event, callback) {
        if (!this.callbacks[event]) {
            this.callbacks[event] = [];
        }
        this.callbacks[event].push(callback);
    }

    off(event, callback) {
        if (this.callbacks[event]) {
            this.callbacks[event] = this.callbacks[event].filter(cb => cb !== callback);
        }
    }

    emit(event, data) {
        if (this.callbacks[event]) {
            this.callbacks[event].forEach(callback => callback(data));
        }
    }
}

export const signalR = new SignalRService();
