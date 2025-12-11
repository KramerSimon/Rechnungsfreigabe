# Rechnungsfreigabe Backend API

Eine .NET 8 Web API für das Rechnungsfreigabe-System (Invoice Approval System).

## Features

- **Authentifizierung & Autorisierung**: JWT-basierte Authentifizierung mit rollenbasierter Autorisierung
- **Rechnungsverwaltung**: Vollständige CRUD-Operationen für Rechnungen mit Workflow-Management
- **Freigabeprozess**: Konfigurierbare Freigaberegeln und automatisierte Workflows
- **Lieferantenverwaltung**: Verwaltung von Lieferantenstammdaten
- **Kostenstellen & Projekte**: Organisationsstruktur mit Budget-Tracking
- **Benachrichtigungen**: Real-time Benachrichtigungssystem
- **Dashboard**: Statistiken und KPIs für das Management
- **Audit Trail**: Vollständige Nachverfolgung aller Änderungen

## Technologien

- **.NET 8**: Moderne Web API Framework
- **Entity Framework Core**: ORM mit MySQL Support
- **MySQL**: Relationale Datenbank
- **JWT Authentication**: Sichere Token-basierte Authentifizierung
- **AutoMapper**: Object-to-Object Mapping
- **Serilog**: Strukturiertes Logging
- **Swagger/OpenAPI**: API-Dokumentation

## Installation & Setup

### Voraussetzungen

- .NET 8 SDK
- MySQL 8.0+
- Visual Studio Code oder Visual Studio 2022

### Schritte

1. **Projektabhängigkeiten installieren:**
   ```bash
   dotnet restore
   ```

2. **Datenbankverbindung konfigurieren:**
   
   Bearbeiten Sie `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=rechnungsfreigabe;User=ihr_benutzer;Password=ihr_passwort;"
     }
   }
   ```

3. **Datenbank erstellen:**
   
   Führen Sie das MySQL-Script aus `../database/mysql_database.sql` aus, um die Datenbank und Beispieldaten zu erstellen.

4. **Anwendung starten:**
   ```bash
   dotnet run
   ```

5. **API testen:**
   
   Die API läuft standardmäßig auf `https://localhost:5001` und die Swagger UI ist verfügbar unter:
   - `https://localhost:5001` (Swagger UI)
   - `https://localhost:5001/swagger` (Alternative URL)

## Konfiguration

### JWT Settings

```json
{
  "JwtSettings": {
    "SecretKey": "IhrSicherheitSchlüsselMindestens32ZeichenLang",
    "Issuer": "RechnungsfreigabeAPI",
    "Audience": "RechnungsfreigabeClient",
    "ExpirationInMinutes": 60
  }
}
```

### CORS Settings

Die API ist für die Angular-Frontend auf `http://localhost:4200` konfiguriert.

## API-Endpunkte

### Authentifizierung
- `POST /api/auth/login` - Benutzer-Login
- `GET /api/auth/me` - Aktuelle Benutzerinformationen
- `POST /api/auth/validate` - Token-Validierung

### Rechnungen
- `GET /api/invoices` - Alle Rechnungen (paginiert)
- `GET /api/invoices/{id}` - Rechnung nach ID
- `POST /api/invoices` - Neue Rechnung erstellen
- `PUT /api/invoices/{id}` - Rechnung aktualisieren
- `DELETE /api/invoices/{id}` - Rechnung löschen (soft delete)
- `GET /api/invoices/dashboard/stats` - Dashboard-Statistiken
- `GET /api/invoices/pending-approvals` - Ausstehende Freigaben
- `POST /api/invoices/{id}/approve` - Rechnung freigeben/ablehnen

### Lieferanten
- `GET /api/suppliers` - Alle Lieferanten
- `GET /api/suppliers/{id}` - Lieferant nach ID
- `POST /api/suppliers` - Neuen Lieferanten erstellen
- `PUT /api/suppliers/{id}` - Lieferanten aktualisieren
- `DELETE /api/suppliers/{id}` - Lieferanten löschen

### Kostenstellen
- `GET /api/costcenters` - Alle Kostenstellen
- `GET /api/costcenters/{id}` - Kostenstelle nach ID
- `GET /api/costcenters/{id}/projects` - Projekte einer Kostenstelle
- `POST /api/costcenters` - Neue Kostenstelle erstellen
- `PUT /api/costcenters/{id}` - Kostenstelle aktualisieren
- `DELETE /api/costcenters/{id}` - Kostenstelle löschen

### Benutzer
- `GET /api/users` - Alle Benutzer
- `GET /api/users/paged` - Benutzer (paginiert)
- `GET /api/users/{id}` - Benutzer nach ID
- `POST /api/users` - Neuen Benutzer erstellen
- `PUT /api/users/{id}` - Benutzer aktualisieren
- `DELETE /api/users/{id}` - Benutzer löschen

### Benachrichtigungen
- `GET /api/notifications` - Benachrichtigungen des aktuellen Benutzers
- `GET /api/notifications/unread-count` - Anzahl ungelesener Benachrichtigungen
- `PATCH /api/notifications/{id}/read` - Benachrichtigung als gelesen markieren
- `PATCH /api/notifications/read-all` - Alle Benachrichtigungen als gelesen markieren

## Datenmodell

### Hauptentitäten
- **Users**: Benutzer mit Rollen und Berechtigungen
- **Invoices**: Rechnungen mit vollständigem Workflow
- **Suppliers**: Lieferantenstammdaten
- **CostCenters**: Kostenstellen mit Budget-Management
- **Projects**: Projekte zugeordnet zu Kostenstellen
- **ApprovalWorkflows**: Freigabeprozesse und -regeln
- **Notifications**: Benachrichtigungssystem

### Wichtige Enums
- **InvoiceStatus**: Eingegangen, In_Pruefung, Freigabe_Erforderlich, Freigegeben, Abgelehnt, Bezahlt, Ueberfaellig, Storniert
- **ApprovalStatus**: Pending, Approved, Rejected, Skipped
- **NotificationPriority**: Low, Normal, High, Urgent

## Sicherheit

- **JWT Authentication**: Sichere Token-basierte Authentifizierung
- **Role-based Authorization**: Rollenbasierte Zugriffskontrolle
- **Data Validation**: Umfassende Eingabevalidierung
- **SQL Injection Protection**: Entity Framework parametrisierte Queries
- **CORS Configuration**: Sichere Cross-Origin-Konfiguration

## Logging

Die Anwendung verwendet Serilog für strukturiertes Logging:
- Console-Output für Entwicklung
- Datei-basiertes Logging mit Rolling-Dateien
- Logs werden im `logs/` Verzeichnis gespeichert

## Entwicklung

### Neue Migration erstellen
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Tests ausführen
```bash
dotnet test
```

### API-Dokumentation generieren
Die Swagger-Dokumentation wird automatisch generiert und ist verfügbar unter `/swagger`.

## Deployment

### Docker (Optional)
```dockerfile
# Dockerfile Beispiel für zukünftige Containerisierung
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "RechnungsfreigabeAPI.dll"]
```

### Produktions-Konfiguration
- Sichere Connection Strings
- HTTPS-Zertifikate konfigurieren
- Logging-Level anpassen
- Performance-Monitoring einrichten

## Beispiel-Login-Daten

Die Datenbank wird mit Beispiel-Benutzern erstellt:
- **admin** - Administrator (alle Berechtigungen)
- **max.mustermann** - Freigeber
- **maria.mueller** - Buchhaltung
- **hans.schmidt** - Manager
- **lisa.klein** - Controller

*Hinweis: In der Entwicklungsversion wird keine Passwort-Authentifizierung implementiert. Für Produktion sollte ein robustes Passwort-System implementiert werden.*

## Support

Bei Fragen oder Problemen wenden Sie sich an das Entwicklungsteam.

## Lizenz

Dieses Projekt ist für interne Nutzung bestimmt.