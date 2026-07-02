import { BrowserRouter } from "react-router-dom";
import AppRouter from "./routes/AppRouter";
import { AuthProvider } from "./context/AuthContext";
import { ChatUnreadProvider } from "./context/ChatUnreadContext";
import { DemoProvider } from "./context/DemoContext";
import CookieBanner from "./components/CookieBanner";
import { AppErrorBoundary } from "./components/common/AppErrorBoundary";
import { ApiErrorToastBridge } from "./components/common/ApiErrorToastBridge";
import { TechnicalDocumentationToastBridge } from "./components/common/TechnicalDocumentationToastBridge";

function App() {
  return (
    <AppErrorBoundary>
      <AuthProvider>
        <ChatUnreadProvider>
          <DemoProvider>
            <BrowserRouter>
              <ApiErrorToastBridge />
              <TechnicalDocumentationToastBridge />
              <AppRouter />
              <CookieBanner />
            </BrowserRouter>
          </DemoProvider>
        </ChatUnreadProvider>
      </AuthProvider>
    </AppErrorBoundary>
  );
}

export default App;
