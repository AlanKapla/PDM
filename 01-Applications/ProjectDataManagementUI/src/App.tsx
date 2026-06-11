import { BrowserRouter } from "react-router-dom";
import AppRouter from "./routes/AppRouter";
import { AuthProvider } from "./context/AuthContext";
import { ChatUnreadProvider } from "./context/ChatUnreadContext";
import { DemoProvider } from "./context/DemoContext";
import CookieBanner from "./components/CookieBanner";

function App() {
  return (
    <AuthProvider>
      <ChatUnreadProvider>
        <DemoProvider>
          <BrowserRouter>
            <AppRouter />
            <CookieBanner />
          </BrowserRouter>
        </DemoProvider>
      </ChatUnreadProvider>
    </AuthProvider>
  );
}

export default App;
