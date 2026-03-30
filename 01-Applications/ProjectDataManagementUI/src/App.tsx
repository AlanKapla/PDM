import { BrowserRouter } from "react-router-dom";
import AppRouter from "./routes/AppRouter";
import { AuthProvider } from "./context/AuthContext";
import { DemoAuthProvider } from "./context/DemoAuthProvider";
import { ProjectCacheProvider } from "./context/ProjectCacheContext";
import { ChatUnreadProvider } from "./context/ChatUnreadContext";
import CookieBanner from "./components/CookieBanner";
import { isMockMode } from "./mocks/index";

const ActiveAuthProvider = isMockMode() ? DemoAuthProvider : AuthProvider;

function App() {
  return (
    <ActiveAuthProvider>
      <ProjectCacheProvider>
        <ChatUnreadProvider>
          <BrowserRouter>
            <AppRouter />
            <CookieBanner />
          </BrowserRouter>
        </ChatUnreadProvider>
      </ProjectCacheProvider>
    </ActiveAuthProvider>
  );
}

export default App;
