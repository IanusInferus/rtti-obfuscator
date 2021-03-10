#include <typeinfo>
#include <cstdio>
#include <unordered_map>
#include <string>
#include <memory>
#include <string.h>

namespace com
{
    namespace test_namespace
    {

        class TestClassA
        {
        public:
            virtual ~TestClassA() {};
        };

        class TestClassB : public TestClassA
        {
        public:
            virtual ~TestClassB() {};
        };
    }
}

#if defined(_MSC_VER)
__declspec(noinline)
#else
__attribute__((noinline))
#endif
static const char * non_inline_str(const char * str) { return str; }

void test(std::shared_ptr<com::test_namespace::TestClassA> a)
{
    auto b = std::make_shared<com::test_namespace::TestClassB>();
    std::printf("%s\n", typeid(b).name()); //display CEAD7865E_________________________________________
    std::printf("%s\n", std::string(typeid(b).name()).c_str()); //display CEAD7865E_______________________________________EE with -O2 and not patched
    std::printf("%s\n", std::string(non_inline_str(typeid(b).name())).c_str()); //display CEAD7865E_________________________________________

    auto pa = a.get();
    std::printf("%s\n", typeid(*pa).name()); //display CE9FC71BE_________________________
    std::printf("%s\n", std::string(typeid(*pa).name()).c_str()); //display CE9FC71BE_________________________
    std::printf("%s\n", std::string(non_inline_str(typeid(*pa).name())).c_str()); //display CE9FC71BE_________________________
}

int main(int argc, char** argv)
{
    auto b = std::make_shared<com::test_namespace::TestClassB>();
    test(b);
    return 0;
}
