# Notes Backend

Simple .NET 8 minimal API providing CRUD endpoints for notes with lightweight JSON-file persistence.

- Swagger UI: http://localhost:3001/docs
- OpenAPI spec: http://localhost:3001/openapi.json
- Health: http://localhost:3001/

## Endpoints

Base path: `/api/notes`

- GET `/api/notes` — list all notes
- GET `/api/notes/{id}` — get a note by id
- POST `/api/notes` — create a note
- PUT `/api/notes/{id}` — update a note
- DELETE `/api/notes/{id}` — delete a note

## Models

Create/Update request:
```
{
  "title": "string (1..200)",
  "content": "string (1..20000)"
}
```

Response:
```
{
  "id": "guid",
  "title": "string",
  "content": "string",
  "createdAtUtc": "ISO-8601 datetime",
  "updatedAtUtc": "ISO-8601 datetime"
}
```

## Quick curl examples

- List:
```
curl -s http://localhost:3001/api/notes
```

- Create:
```
curl -s -X POST http://localhost:3001/api/notes \
  -H "Content-Type: application/json" \
  -d '{"title":"First","content":"Hello notes"}'
```

- Get:
```
curl -s http://localhost:3001/api/notes/{id}
```

- Update:
```
curl -s -X PUT http://localhost:3001/api/notes/{id} \
  -H "Content-Type: application/json" \
  -d '{"title":"Updated title","content":"Updated content"}' -i
```

- Delete:
```
curl -s -X DELETE http://localhost:3001/api/notes/{id} -i
```

## Persistence

Notes are stored in a JSON file at `App_Data/notes.json` under the app content root by default.
You can override the path with configuration key `Notes:DataFile` in `appsettings.json`:

```
{
  "Notes": {
    "DataFile": "/absolute/or/relative/path/to/notes.json"
  }
}
```

## CORS

CORS is configured to allow all origins/methods/headers for ease of local development.

