# Login Component Documentation

## Overview
This document describes the login component implementation with backend connection for the Rechnungsfreigabe application.

## Features
- ✅ Reactive login form with validation
- ✅ JWT token-based authentication
- ✅ Auto token persistence in localStorage
- ✅ Authentication state management
- ✅ Route protection with auth guards
- ✅ Auto-redirect after login/logout
- ✅ User information display in navigation
- ✅ Material Design UI components
- ✅ Responsive design
- ✅ Error handling and user feedback

## Components Created

### 1. Auth Models (`/core/models/auth.models.ts`)
- `LoginRequest` - Login credentials interface
- `LoginResponse` - API login response interface
- `User` - User data interface
- `Role` & `Permission` - Authorization interfaces
- `AuthState` - Authentication state interface

### 2. Auth Service (`/core/services/auth.service.ts`)
- Handles login/logout operations
- JWT token management
- Authentication state management
- API communication with backend
- Token validation
- Persistent authentication across browser sessions

### 3. Auth Interceptor (`/core/services/auth.interceptor.ts`)
- Automatically adds JWT tokens to HTTP requests
- Handles authorization headers

### 4. Auth Guard (`/core/guards/auth.guard.ts`)
- Protects routes from unauthorized access
- Redirects to login when not authenticated

### 5. Login Component (`/features/login/login.component.ts`)
- Reactive form with username/password fields
- Form validation and error handling
- Loading states and user feedback
- Material Design components
- Responsive design

## Backend Integration

### Expected API Endpoints
The component expects these backend endpoints:

1. **POST /api/auth/login**
   ```json
   // Request
   {
     "username": "string",
     "password": "string"
   }
   
   // Response
   {
     "token": "string",
     "user": {
       "id": 1,
       "username": "string",
       "email": "string",
       "firstName": "string",
       "lastName": "string",
       "fullName": "string",
       "isActive": true,
       "createdAt": "2023-01-01T00:00:00Z",
       "roles": [...]
     },
     "permissions": ["string"]
   }
   ```

2. **POST /api/auth/validate**
   ```json
   // Request: Bearer token in body
   "token_string"
   
   // Response
   {
     "valid": true
   }
   ```

3. **GET /api/auth/me** (with Authorization header)
   ```json
   // Response: User object
   ```

## Configuration

### Environment Configuration
Update `/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api'  // Your backend URL
};
```

### Backend CORS Configuration
Ensure your backend allows CORS from the frontend origin:
```csharp
// In your ASP.NET Core backend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular dev server
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

## Usage

### Starting the Application
1. **Start the backend server**:
   ```bash
   cd backend
   dotnet run
   ```

2. **Start the frontend development server**:
   ```bash
   cd frontend
   ng serve
   ```

3. **Access the application**:
   - Navigate to `http://localhost:4200`
   - You'll be redirected to `/login` if not authenticated
   - After successful login, you'll be redirected to `/dashboard`

### Testing Login
Use any valid user credentials from your backend database. Example:
- Username: `admin`
- Password: `password123`

## Route Protection
All routes except `/login` are protected by the auth guard:
- `/dashboard` - Requires authentication
- `/invoice/:id` - Requires authentication  
- `/admin/rules` - Requires authentication
- `/**` - Redirects to `/login`

## User Experience Features

### Loading States
- Login button shows spinner during authentication
- Form is disabled while processing

### Error Handling
- Invalid credentials: "Invalid username or password"
- Server errors: "Server error. Please try again later"
- Network errors: "Login failed. Please try again"

### Responsive Design
- Mobile-friendly login form
- Adaptive layout for different screen sizes

### Logout Functionality
- Accessible from user menu in navigation
- Clears authentication state and redirects to login

## Security Features

### Token Management
- JWT tokens stored in localStorage
- Automatic token inclusion in API requests
- Token validation on application start
- Automatic logout on token expiration

### Route Protection
- Auth guard prevents access to protected routes
- Automatic redirection to login when not authenticated
- Persistent authentication across browser sessions

## Customization

### Styling
Modify `/features/login/login.component.scss` to customize appearance:
- Colors and gradients
- Card styling and shadows
- Form field appearance
- Button styles

### Validation
Update validation rules in the login component:
```typescript
this.loginForm = this.formBuilder.group({
  username: ['', [Validators.required, Validators.minLength(3)]],
  password: ['', [Validators.required, Validators.minLength(8)]]
});
```

### API Configuration
Update the API URL in the auth service or environment files to match your backend configuration.

## Troubleshooting

### Common Issues
1. **CORS errors**: Ensure backend CORS is configured correctly
2. **401 Unauthorized**: Check username/password and backend authentication
3. **Network errors**: Verify backend is running and accessible
4. **Route not found**: Ensure all components are properly imported in routes

### Debug Tips
- Check browser network tab for API requests
- Inspect localStorage for stored tokens
- Check console for JavaScript errors
- Verify backend logs for authentication issues
