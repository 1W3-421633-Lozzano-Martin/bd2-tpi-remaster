# WatchParty

Aplicación web para ver películas en grupo con sincronización de video, chat en vivo y votación.

## Stack Tecnológico

- **Backend**: C# .NET 8.0
- **Frontend**: HTML, CSS, JavaScript (Vanilla)
- **Base de Datos**: MongoDB
- **Cache en Tiempo Real**: Redis
- **Comunicación en Tiempo Real**: SignalR (WebSocket)

## Estructura del Proyecto

```
WatchParty/
├── WatchParty.Backend/          # API .NET
│   ├── Controllers/             # Controladores API
│   ├── Hubs/                    # SignalR Hubs
│   ├── Models/                  # Modelos MongoDB
│   ├── DTOs/                    # Data Transfer Objects
│   ├── Repositories/            # Patrón Repository
│   ├── Services/                # Lógica de negocio
│   ├── Configuration/           # Configuración
│   ├── config.yaml              # Configuración YAML
│   └── Program.cs               # Punto de entrada
├── WatchParty.Frontend/         # Frontend estático
│   ├── index.html               # Página principal
│   ├── *.html                   # Páginas HTML
│   ├── css/                     # Estilos
│   └── js/                      # JavaScript modular
├── docker-compose.yml           # Docker Compose
└── README.md                    # Este archivo
```

## Características

- ✅ Crear salas de visualización
- ✅ Unirse a salas por código, enlace o QR
- ✅ Sincronización de video en tiempo real
- ✅ Chat en vivo
- ✅ Contador de espectadores
- ✅ Sistema de votación para cambiar película
- ✅ Registro e inicio de sesión
- ✅ Salas privadas con contraseña
- ✅ Gestión de películas

## Configuración

### Variables de Entorno (Backend)

```env
MONGO_URI=mongodb://localhost:27017
REDIS_URL=localhost:6379
JWT_SECRET=YourSuperSecretKeyThatShouldBeAtLeast32Characters!
ASPNETCORE_ENVIRONMENT=Production
```

### config.yaml (Backend)

```yaml
jwt:
  secret: "YourSuperSecretKey..."
  issuer: "WatchParty"
  audience: "WatchParty"
  expirationDays: 7

mongodb:
  connectionString: "mongodb://localhost:27017"
  databaseName: "watchparty"

redis:
  connectionString: "localhost:6379"

app:
  apiUrl: "http://localhost:5000"
  frontendUrl: "http://localhost:3000"
```

## Ejecución Local

### Opción 1: Docker Compose (Recomendado)

```bash
docker-compose up -d
```

Esto iniciarán:
- MongoDB en puerto 27017
- Redis en puerto 6379
- API en puerto 5000
- Frontend en puerto 3000

### Opción 2: Desarrollo Manual

1. **Backend**:
```bash
cd WatchParty.Backend
dotnet restore
dotnet run
```

2. **Frontend**:
```bash
cd WatchParty.Frontend
# Usar un servidor estático como:
npx serve .
# O
python -m http.server 3000
```

## Despliegue en Railway

### 1. Preparar el repositorio

```bash
# En Railway, crear nuevo proyecto
# Conectar repositorio de Git
```

### 2. Configurar variables de entorno

En Railway, añadir:

```
MONGO_URI=mongodb+srv://user:pass@cluster.mongodb.net/watchparty
REDIS_URL=redis-xxxx.xx.cloud.redislabs.com:xxxxx
JWT_SECRET=<generar-secreto-largo>
ASPNETCORE_ENVIRONMENT=Production
PORT=8080
```

### 3. Configuración de Railway

El archivo `railway.json` está configurado para:
- Build: `dotnet publish -c Release`
- Start: `dotnet WatchParty.Backend.dll`
- Puerto: 8080

### 4. Despliegue del Frontend

El frontend es estático y puede desplegarse en:
- Vercel
- Netlify
- GitHub Pages
- Cloudflare Pages

Actualizar `js/utils/config.js` con la URL de la API:

```javascript
const API_URL = 'https://tu-api.railway.app/api';
const HUB_URL = 'https://tu-api.railway.app/hubs/watchparty';
```

## API Endpoints

### Autenticación
- `POST /api/auth/register` - Registro
- `POST /api/auth/login` - Inicio de sesión
- `GET /api/auth/me` - Usuario actual

### Salas
- `GET /api/rooms` - Listar salas activas
- `GET /api/rooms/{code}` - Obtener sala
- `POST /api/rooms` - Crear sala
- `POST /api/rooms/{code}/join` - Unirse a sala
- `PUT /api/rooms/{code}` - Actualizar sala
- `DELETE /api/rooms/{code}` - Eliminar sala

### Películas
- `GET /api/movies` - Buscar películas
- `GET /api/movies/popular` - Películas populares
- `GET /api/movies/{id}` - Obtener película
- `POST /api/movies` - Añadir película

### SignalR Hub
- `POST /hubs/watchparty` - WebSocket hub

## SignalR Events

### Cliente → Servidor
- `JoinRoom(code, password)` - Unirse a sala
- `LeaveRoom(code)` - Salir de sala
- `SendMessage(content, type)` - Enviar mensaje
- `SyncVideo(videoUrl, position, isPlaying)` - Sincronizar video
- `ChangeMovie(movieId, title, thumbnail)` - Cambiar película
- `StartVote(movieId, title, duration)` - Iniciar votación
- `CastVote(movieId)` - Votar
- `EndVote()` - Terminar votación

### Servidor → Cliente
- `RoomJoined(data)` - Sala unida
- `ViewerJoined(data)` - Nuevo espectador
- `ViewerLeft(data)` - Espectador sale
- `NewMessage(message)` - Nuevo mensaje
- `VideoSync(data)` - Sincronización de video
- `MovieChanged(data)` - Película cambiada
- `VoteStarted(data)` - Votación iniciada
- `VoteUpdated(data)` - Voto actualizado
- `VoteEnded(data)` - Votación terminada
- `Error(message)` - Error

## Licencia

MIT
