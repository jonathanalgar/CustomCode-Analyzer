### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NonPublicInterface | Design | Warning | Interface visibility analyzer
NoSingleInterface | Design | Warning | Interface requirement analyzer
ManyInterfaces | Design | Warning | Interface count analyzer
NameBeginsWithUnderscores | Naming | Warning | Naming convention analyzer
NoImplementingClass | Design | Warning | Checks that OSInterface has implementing class
NoParameterlessConstructor | Design | Warning | Checks that OSInterface has implementing class
EmptyInterface | Design | Warning | The interface decorated with OSInterface must define at least one method
MultipleImplementations | Design | Warning | Multiple implementations of OSInterface
NonPublicImplementation | Design | Warning | Implementation of OSInterface must be public
NonPublicStruct | Design | Warning | Structs decorated with OSStructure must be public
NonPublicStructureField | Design | Warning | Fields in structs decorated with OSStructure must be public
NonPublicIgnoredField | Design | Warning | Properties and fields decorated with OSIgnore must be public
NoPublicMembers | Design | Warning | Checks that OSInterface has public members
DuplicateStructureName | Design | Warning | Checks that OSStructure has unique names
ReferenceParameter | Design | Warning | Checks that OSInterface has reference parameters
NameTooLong | Naming | Warning | Checks that names are not too long
NameStartsWithNumber | Naming | Warning | Checks that names don't start with number
InvalidCharactersInName | Naming | Warning | Checks that names don't contain invalid characters