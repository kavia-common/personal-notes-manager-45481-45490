# Personal Notes Backend API

This service provides a simple RESTful API to manage personal notes using in-memory storage.

- API Docs (Swagger/NSwag UI): GET /docs
- OpenAPI JSON: GET /openapi.json
- Health: GET /

## Endpoints

- GET /api/notes
  - List all notes
  - 200 OK: Array of Note

- GET /api/notes/{id}
  - Get a single note by GUID
  - 200 OK: Note
  - 404 Not Found

- POST /api/notes
  - Create a note
  - Body: { "title": "string", "content": "string" }
  - 201 Created: Note
  - 400 ValidationProblem

- PUT /api/notes/{id}
  - Update a note
  - Body: { "title": "string", "content": "string" }
  - 200 OK: Note
  - 400 ValidationProblem
  - 404 Not Found

- DELETE /api/notes/{id}
  - Delete a note
  - 204 No Content
  - 404 Not Found

## Notes

- Storage is in-memory and resets when the process restarts.
- CORS is open to simplify local development.
- App targets .NET 8 minimal APIs and uses NSwag for OpenAPI.

## Run locally

- HTTP: http://localhost:3001
- Docs UI: http://localhost:3001/docs
- OpenAPI JSON: http://localhost:3001/openapi.json
