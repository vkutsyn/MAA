import { createBrowserRouter, RouteObject } from 'react-router-dom'
import App from '../App'
import { WizardLandingRoute } from './WizardLandingRoute'
import { WizardRoute } from './WizardRoute'

// Define routes
const routes: RouteObject[] = [
  {
    path: '/',
    element: <App />,
    children: [
      {
        index: true,
        element: <WizardLandingRoute />,
      },
      {
        path: 'wizard',
        element: <WizardRoute />,
      },
    ],
  },
]

// Create router
export const router = createBrowserRouter(routes)
