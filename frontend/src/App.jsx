import { Route, Routes } from "react-router-dom";
import Login from "./pages/Login";
import Test from "./features/test/Test";

function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />

      <Route path="/test" element={<Test />} />

    </Routes>
  )
}

export default App