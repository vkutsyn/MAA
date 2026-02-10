import { createBrowserRouter, RouteObject } from 'react-router-dom'
import App from '../App'

// Define routes
const routes: RouteObject[] = [
  {
    path: '/',
    element: <App />,
    children: [
      {
        index: true,
        lazy: async () => {
          // Placeholder for landing page - will be implemented in Phase 3
          return { Component: () => <div>Landing Page - Coming Soon</div> }
        },
      },
      {
        path: 'wizard',
        lazy: async () => {
          // Placeholder for wizard - will be implemented in Phase 3+
          return { Component: () => <div>Wizard - Coming Soon</div> }
        },
      },
    ],
  },
]

// Create router
export const router = createBrowserRouter(routes)
