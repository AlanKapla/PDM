import { BrowserRouter } from "react-router-dom";
import AppRouter from "./routes/AppRouter";
import { AuthProvider } from "./context/AuthContext";
import { ChatUnreadProvider } from "./context/ChatUnreadContext";
import CookieBanner from "./components/CookieBanner";

function App() {
  return (
    <AuthProvider>
      <ChatUnreadProvider>
        <BrowserRouter>
          <AppRouter />
          <CookieBanner />
        </BrowserRouter>
      </ChatUnreadProvider>
    </AuthProvider>
  );
}

export default App;
