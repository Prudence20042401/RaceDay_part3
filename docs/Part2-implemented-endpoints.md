# RaceDay API - implemented endpoints (Part 2)

Role: **None** = public, **Any** = any logged-in user, otherwise that role only.
All Organiser endpoints that touch an event also check the event belongs to the logged-in Organiser.
Common failures not repeated in every row: `401 Unauthorized` (not logged in), `403 Forbidden` (wrong role / not the owner), `400 Bad Request` (invalid body).

| Method | Route | Description | Role | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Create an Organiser or Participant account | None | email, password, firstName, lastName, role, (Organiser: organisationName, phoneNumber, inviteCode) (Participant: dateOfBirth) | 200 OK user summary; 400 invalid; 403 bad invite code; 409 email exists |
| POST | /api/auth/login | Log in and start the server-side session | None | email, password | 200 OK user summary; 401 wrong credentials |
| GET | /api/auth/me | Show the user ID and role stored in the session | Any | None | 200 OK { userID, role }; 401 |
| POST | /api/auth/logout | Clear the session and log out | Any | None | 200 OK message |
| POST | /api/auth/forgot-password | Request a password reset link (valid 1 hour, single use) | None | email | 200 OK generic message (same whether or not the email exists); 400 invalid email |
| POST | /api/auth/reset-password | Set a new password using the token from the reset link | None | token, newPassword | 200 OK message; 400 invalid/expired token or password under 8 characters |
| GET | /api/profile | View own profile | Any | None | 200 OK profile; 401; 404 |
| PUT | /api/profile | Update own profile | Any | firstName, lastName, (Organiser: organisationName, phoneNumber) (Participant: dateOfBirth) | 200 OK message; 400; 401; 404 |
| GET | /api/events | List all events | None | None | 200 OK list of events |
| GET | /api/events/{id} | Get one event | None | None | 200 OK event; 404 |
| GET | /api/events/mine | List the Organiser's own events | Organiser | None | 200 OK list of events |
| POST | /api/events | Create an event | Organiser | name, description, eventDate, location, distance, eventType (Run/Walk/Cycle) | 201 Created event; 400 |
| PUT | /api/events/{id} | Update own event | Organiser | same as create | 200 OK event; 403; 404 |
| DELETE | /api/events/{id} | Delete own event | Organiser | None | 200 OK message; 403; 404; 409 has enrolments |
| GET | /api/categories | List all categories | None | None | 200 OK list |
| GET | /api/categories/event/{eventId} | List categories for one event | None | None | 200 OK list; 404 |
| POST | /api/categories | Add a category to own event | Organiser | eventID, name, description | 200 OK category; 403; 404 |
| PUT | /api/categories/{id} | Update a category | Organiser | eventID (unchanged), name, description | 200 OK category; 400; 403; 404 |
| DELETE | /api/categories/{id} | Delete a category | Organiser | None | 200 OK message; 403; 404; 409 has enrolments |
| POST | /api/enrolments | Enter an event in a category (status starts Pending) | Participant | eventID, categoryID | 200 OK enrolment; 400; 409 already enrolled |
| GET | /api/enrolments/my | List own enrolments | Participant | None | 200 OK list |
| GET | /api/enrolments/event/{eventId} | List all enrolments for own event | Organiser | None | 200 OK list; 403 |
| PUT | /api/enrolments/{id}/status | Set status Pending/Confirmed/Cancelled | Organiser | status | 200 OK enrolment; 400; 403; 404 |
| POST | /api/results | Record finish time and position | Organiser | enrolmentID, finishTime (hh:mm:ss), finishingPosition | 200 OK result; 400; 403; 404; 409 exists |
| GET | /api/results/my | List own results (event, category, time, position, total finishers) | Participant | None | 200 OK list |
| GET | /api/results/event/{eventId} | List results for own event | Organiser | None | 200 OK list; 403; 404 |
| PUT | /api/results/{id} | Correct a result | Organiser | finishTime, finishingPosition | 200 OK result; 403; 404 |
| DELETE | /api/results/{id} | Delete a result | Organiser | None | 200 OK message; 403; 404 |
