import { createBrowserRouter, RouteObject } from "react-router-dom";
import App from "../App";
import { LoginRoute } from "./LoginRoute";
import { RegisterRoute } from "./RegisterRoute";
import { WizardLandingRoute } from "./WizardLandingRoute";
import { WizardRoute } from "./WizardRoute";
import { StateContextStep } from "./StateContextStep";
import { EligibilityResultRoute } from "./EligibilityResultRoute";

// Define routes
const routes: RouteObject[] = [
  {
    path: "/",
    element: <App />,
    children: [
      {
        index: true,
        element: <WizardLandingRoute />,
      },
      {
        path: "login",
        element: <LoginRoute />,
      },
      {
        path: "register",
        element: <RegisterRoute />,
      },
      {
        path: "state-context",
        element: <StateContextStep />,
      },
      {
        path: "wizard",
        element: <WizardRoute />,
      },
      {
        path: "results",
        element: <EligibilityResultRoute />,
      },
    ],
  },
];

// Create router
export const router = createBrowserRouter(routes);
