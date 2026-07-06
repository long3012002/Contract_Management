import { Route, Routes } from "react-router-dom";
import Login from "./pages/Login";
import CheckAuth from "./features/test/CheckAuth";
import Test from "./features/test/Test";

function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route element={<CheckAuth />}>
        <Route path="/test" element={<Test />} />
      </Route>
    </Routes>
  )
}

export default App