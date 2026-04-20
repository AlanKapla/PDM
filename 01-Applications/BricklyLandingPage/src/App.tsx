import Navbar from './components/Navbar'
import Hero from './components/Hero'
import About from './components/About'
import Modules from './components/Modules'
import TargetUsers from './components/TargetUsers'
import CallToAction from './components/CallToAction'
import Footer from './components/Footer'

export default function App() {
  return (
    <>
      <Navbar />
      <main>
        <Hero />
        <About />
        <Modules />
        <TargetUsers />
        <CallToAction />
      </main>
      <Footer />
    </>
  )
}
